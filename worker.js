export default {
    async fetch(request, env) {
        const url = new URL(request.url);

        if (url.pathname === "/api/generate" && request.method === "POST") {
            try {
                const body = await request.json();
                const userPrompt = body.userPrompt || "";
                const rawTelemetry = body.rawTelemetry || "";
                const goldStandard = body.goldStandard || "";

                if (!userPrompt || !rawTelemetry) {
                    return new Response(JSON.stringify({ error: "Missing userPrompt or rawTelemetry payload." }), {
                        status: 400,
                        headers: { "Content-Type": "application/json" }
                    });
                }

                // -------------------------------------------------------------
                // PIPELINE STAGE 1: Execution Sandbox (Llama-3-8B)
                // -------------------------------------------------------------
                const executionSystemPrompt = `You are a strict data processing node. You must process the raw data strictly adhering to the user's instructions. Do not provide any conversational filler or meta-commentary. Run the instruction exactly.`;
                
                const executionResponse = await env.AI.run("@cf/meta/llama-3-8b-instruct", {
                    messages: [
                        { role: "system", content: executionSystemPrompt },
                        { role: "user", content: `Raw Data:\n${rawTelemetry}\n\nOperator Instructions:\n${userPrompt}` }
                    ]
                });

                const executionResult = executionResponse.response;

                // -------------------------------------------------------------
                // PIPELINE STAGE 2: Calibrated Decoupled Evaluator (Mistral-7B)
                // -------------------------------------------------------------
                const evaluationSystemPrompt = `You are an expert calibrated AI judge assessing prompt engineering outputs. 
You will be provided with:
- RAW TELEMETRY
- OPERATOR PROMPT
- ACTUAL AI EXECUTION RESULT
- TARGET GOLD STANDARD ANSWER

Your goal is to evaluate how successfully the ACTUAL AI EXECUTION RESULT satisfied the target outcomes of the GOLD STANDARD, given the constraints of the OPERATOR PROMPT.

You must respond in raw JSON format. Do not write any markdown codeblocks or conversational text. Return exactly this JSON structure:
{
  "semantic_alignment_score": <float between 0.00 and 1.00 indicating quality and correctness alignment vs the Gold Standard>,
  "failure_analysis": "<Brief string explaining what was missing or incorrect. If it is a perfect match, return 'None'>"
}

Be extremely strict. 
- If the ACTUAL RESULT has emotional venting or corporate filler that was in the RAW TELEMETRY but should have been cleaned, deduct score.
- If it missed tagging missing variables correctly (e.g. tag empty owner slots with '[UNASSIGNED]' or similar), deduct score.
- If it drifted semantically from the GOLD STANDARD, scale the score down proportionally.`;

                const evaluationUserMessage = `RAW TELEMETRY:
"${rawTelemetry}"

OPERATOR PROMPT:
"${userPrompt}"

ACTUAL AI EXECUTION RESULT:
"${executionResult}"

TARGET GOLD STANDARD ANSWER:
"${goldStandard}"`;

                const evaluationResponse = await env.AI.run("@cf/mistral/mistral-7b-instruct-v0.2", {
                    messages: [
                        { role: "system", content: evaluationSystemPrompt },
                        { role: "user", content: evaluationUserMessage }
                    ]
                });

                // Parse the JSON output returned by the judge model
                let judgeText = evaluationResponse.response.trim();
                
                // Clean any markdown formatting the model might have wrapped it in
                if (judgeText.startsWith("```")) {
                    judgeText = judgeText.replace(/^```json\s*/i, "").replace(/```$/, "").trim();
                }

                let score = 0.0;
                let feedback = "Failed to parse judge evaluation.";
                
                try {
                    const parsedJudge = JSON.parse(judgeText);
                    score = parseFloat(parsedJudge.semantic_alignment_score);
                    if (isNaN(score)) score = 0.0;
                    score = Math.max(0.0, Math.min(1.0, score)); // Clamp 0.0-1.0
                    feedback = parsedJudge.failure_analysis || "No analysis provided.";
                } catch (parseErr) {
                    // Fallback heuristics if the LLM output is not valid JSON
                    feedback = "Evaluator output parse error. Raw judge output: " + judgeText;
                    
                    const scoreMatch = judgeText.match(/"semantic_alignment_score"\s*:\s*([0-9\.]+)/);
                    if (scoreMatch) {
                        score = parseFloat(scoreMatch[1]);
                        if (isNaN(score)) score = 0.0;
                    }
                    const feedbackMatch = judgeText.match(/"failure_analysis"\s*:\s*"([^"]+)"/);
                    if (feedbackMatch) {
                        feedback = feedbackMatch[1];
                    }
                }

                return new Response(JSON.stringify({
                    execution_result: executionResult,
                    semantic_alignment_score: score,
                    failure_analysis: feedback
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
