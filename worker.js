export default {
    async fetch(request, env) {
        const url = new URL(request.url);

        if (url.pathname === "/api/generate" && request.method === "POST") {
            try {
                const body = await request.json();
                const userPrompt = body.userPrompt || "";
                const rawTelemetry = body.rawTelemetry || "";

                if (!userPrompt || !rawTelemetry) {
                    return new Response(JSON.stringify({ error: "Missing userPrompt or rawTelemetry payload." }), {
                        status: 400,
                        headers: { "Content-Type": "application/json" }
                    });
                }

                // Stage 1 Execution Sandbox using llama-3.2-3b-instruct (cheap, fast, interactive)
                const executionSystemPrompt = `You are a strict, bare-metal execution node. Process the raw data strictly according to Operator Instructions. 
CRITICAL RULES:
- Output ONLY the direct resolution (code, commands, or data).
- ZERO conversational filler. ZERO intro/outro text.
- Do not wrap the output in markdown unless explicitly requested.`;
                
                const executionResponse = await env.AI.run("@cf/meta/llama-3.2-3b-instruct", {
                    messages: [
                        { role: "system", content: executionSystemPrompt },
                        { role: "user", content: `Raw Data:\n${rawTelemetry}\n\nOperator Instructions:\n${userPrompt}` }
                    ]
                });

                const executionResult = executionResponse.response;

                return new Response(JSON.stringify({
                    execution_result: executionResult
                }), {
                    headers: { "Content-Type": "application/json" }
                });

            } catch (err) {
                return new Response(JSON.stringify({ error: err.message }), {
                    status: 500,
                    headers: { "Content-Type": "application/json" }
                });
            }
        }

        if (url.pathname === "/api/evaluate_batch" && request.method === "POST") {
            try {
                const body = await request.json();
                const items = body.items || [];

                if (items.length === 0) {
                    return new Response(JSON.stringify({ error: "No items provided for evaluation." }), {
                        status: 400,
                        headers: { "Content-Type": "application/json" }
                    });
                }

                // Batch evaluation system prompt for Qwen or Llama 3.1 8B
                const evaluationSystemPrompt = `You are an expert calibrated AI judge assessing the quality of the agent's output across a series of circumstantial challenges.
For each challenge, you are provided with its ID, the circumstance problem description, the operator prompt, the actual agent execution result, and the target gold standard outcome.

Grade each challenge and return a JSON array matching exactly this JSON structure, with absolutely zero markdown codeblocks or conversational filler:
[
  {
    "challenge_id": "<ID of the challenge>",
    "failure_analysis": "<Briefly analyze if the ACTUAL RESULT successfully resolves the CIRCUMSTANCE. Return 'None' if successful>",
    "actionability_score": <float between 0.00 and 1.00 indicating if the output contains direct usable code, commands, or clear workflows with zero translation needed>,
    "target_alignment_score": <float between 0.00 and 1.00 indicating if the output logically resolves the specific symptom or goal described in the circumstance>
  }
]

Be fair:
- If no specific files or variables were provided in the circumstance, DO NOT deduct points for the agent using logical hypothetical files/examples to illustrate the solution.
- Only deduct from target_alignment_score if the response fails to address the root problem.`;

                let evaluationUserMessage = "Evaluate the following challenges:\n\n";
                for (let i = 0; i < items.length; i++) {
                    const item = items[i];
                    evaluationUserMessage += `---
CHALLENGE INDEX: ${i + 1}
CHALLENGE ID: "${item.id}"
THE CIRCUMSTANCE:
"${item.rawTelemetry}"

OPERATOR PROMPT:
"${item.userPrompt}"

ACTUAL AI EXECUTION RESULT:
"${item.executionResult}"

TARGET GOLD STANDARD OUTCOME:
"${item.goldStandard}"\n\n`;
                }

                const evaluationResponse = await env.AI.run("@cf/meta/llama-3.1-8b-instruct", {
                    messages: [
                        { role: "system", content: evaluationSystemPrompt },
                        { role: "user", content: evaluationUserMessage }
                    ]
                });

                if (!evaluationResponse || typeof evaluationResponse.response !== 'string') {
                    throw new Error(`Invalid AI response: ${JSON.stringify(evaluationResponse)}`);
                }

                let judgeText = evaluationResponse.response.trim();
                if (judgeText.startsWith("```")) {
                    judgeText = judgeText.replace(/^```json\s*/i, "").replace(/```$/, "").trim();
                }

                let parsedResults = [];
                try {
                    parsedResults = JSON.parse(judgeText);
                } catch (parseErr) {
                    const arrayMatch = judgeText.match(/\[[\s\S]*\]/);
                    if (arrayMatch) {
                        try {
                            parsedResults = JSON.parse(arrayMatch[0]);
                        } catch (e) {
                            throw new Error("Failed to parse judge output: " + judgeText);
                        }
                    } else {
                        throw new Error("Failed to parse judge output: " + judgeText);
                    }
                }

                return new Response(JSON.stringify({
                    results: parsedResults
                }), {
                    headers: { "Content-Type": "application/json" }
                });

            } catch (err) {
                return new Response(JSON.stringify({ error: err.message }), {
                    status: 500,
                    headers: { "Content-Type": "application/json" }
                });
            }
        }

        return env.ASSETS.fetch(request);
    }
};
