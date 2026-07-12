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
                // PIPELINE STAGE 1: Execution Sandbox (Llama-4-Scout)
                // -------------------------------------------------------------
                const executionSystemPrompt = `You are a strict, bare-metal execution node. Process the raw data strictly according to Operator Instructions. 
CRITICAL RULES:
- Output ONLY the direct resolution (code, commands, or data).
- ZERO conversational filler. ZERO intro/outro text.
- Do not wrap the output in markdown unless explicitly requested.`;
                
                const executionResponse = await env.AI.run("@cf/meta/llama-4-scout-17b-16e-instruct", {
                    messages: [
                        { role: "system", content: executionSystemPrompt },
                        { role: "user", content: `Raw Data:\n${rawTelemetry}\n\nOperator Instructions:\n${userPrompt}` }
                    ]
                });

                const executionResult = executionResponse.response;

                // -------------------------------------------------------------
                // PIPELINE STAGE 2: Real-World Resolution Judge (QwQ-32B)
                // -------------------------------------------------------------
                const evaluationSystemPrompt = `You are an expert calibrated AI judge assessing the quality of instructions given to an agent to solve a specific circumstance.
You will be provided with:
- THE CIRCUMSTANCE (The raw observed symptom or problem)
- OPERATOR PROMPT (The instruction written by the user)
- ACTUAL AI EXECUTION RESULT (What the agent produced)
- TARGET GOLD STANDARD OUTCOME (The expected result/resolution)

Your goal is to evaluate the ACTUAL AI EXECUTION RESULT and grade how effectively the OPERATOR PROMPT guided the agent to achieve the TARGET GOLD STANDARD OUTCOME under the following three criteria.

You must respond in raw JSON format. Do not write any markdown codeblocks or conversational text. Return exactly this JSON structure:
{
  "failure_analysis": "<Analyze the ACTUAL RESULT against the GOLD STANDARD first. Detail mismatches here. If perfect, return 'None'>",
  "actionability_score": <float between 0.00 and 1.00 indicating if the output contains a direct, immediately usable solution like executable code blocks, CLI commands, or direct physical workflows with zero translation needed>,
  "constraint_adherence_score": <float between 0.00 and 1.00 indicating if the output strictly respected all parameters, dimensions, files, or environment constraints in the circumstance and prompt, with zero hallucinations>,
  "target_alignment_score": <float between 0.00 and 1.00 indicating if the output directly resolves the specific symptom or goal described by the user>
}

Example JSON Response:
{
  "failure_analysis": "The agent provided a Python script instead of a shell script.",
  "actionability_score": 0.5,
  "constraint_adherence_score": 0.0,
  "target_alignment_score": 0.9
}

Be extremely strict:
- If the ACTUAL RESULT contains conversational intro/outro filler (e.g. "Sure, I can help with that..."), deduct from the actionability_score.
- If it hallucinated elements or ignored constraints (e.g. board sizes, directory limits), deduct heavily from constraint_adherence_score.
- If it drifted semantically from solving the primary circumstance, deduct from target_alignment_score.`;

                const evaluationUserMessage = `THE CIRCUMSTANCE:
"${rawTelemetry}"

OPERATOR PROMPT:
"${userPrompt}"

ACTUAL AI EXECUTION RESULT:
"${executionResult}"

TARGET GOLD STANDARD OUTCOME:
"${goldStandard}"`;

                const evaluationResponse = await env.AI.run("@cf/qwen/qwq-32b", {
                    messages: [
                        { role: "system", content: evaluationSystemPrompt },
                        { role: "user", content: evaluationUserMessage }
                    ]
                });

                if (!evaluationResponse || typeof evaluationResponse.response !== 'string') {
                    throw new Error(`Invalid AI response: ${JSON.stringify(evaluationResponse)}`);
                }

                // Parse the JSON output returned by the judge model
                let judgeText = evaluationResponse.response.trim();
                
                // Clean any markdown formatting the model might have wrapped it in
                if (judgeText.startsWith("```")) {
                    judgeText = judgeText.replace(/^```json\s*/i, "").replace(/```$/, "").trim();
                }

                let actionability = 0.0;
                let constraintAdherence = 0.0;
                let targetAlignment = 0.0;
                let feedback = "Failed to parse judge evaluation.";
                
                try {
                    const parsedJudge = JSON.parse(judgeText);
                    actionability = parseFloat(parsedJudge.actionability_score);
                    constraintAdherence = parseFloat(parsedJudge.constraint_adherence_score);
                    targetAlignment = parseFloat(parsedJudge.target_alignment_score);
                    feedback = parsedJudge.failure_analysis || "No analysis provided.";
                } catch (parseErr) {
                    // Fallback heuristics if the LLM output is not valid JSON
                    feedback = "Evaluator output parse error. Raw judge output: " + judgeText;
                    
                    const actionMatch = judgeText.match(/"actionability_score"\s*:\s*([0-9\.]+)/);
                    if (actionMatch) actionability = parseFloat(actionMatch[1]);
                    
                    const constraintMatch = judgeText.match(/"constraint_adherence_score"\s*:\s*([0-9\.]+)/);
                    if (constraintMatch) constraintAdherence = parseFloat(constraintMatch[1]);
                    
                    const targetMatch = judgeText.match(/"target_alignment_score"\s*:\s*([0-9\.]+)/);
                    if (targetMatch) targetAlignment = parseFloat(targetMatch[1]);
                }

                // Clamp scores 0.0-1.0
                actionability = Math.max(0.0, Math.min(1.0, isNaN(actionability) ? 0.0 : actionability));
                constraintAdherence = Math.max(0.0, Math.min(1.0, isNaN(constraintAdherence) ? 0.0 : constraintAdherence));
                targetAlignment = Math.max(0.0, Math.min(1.0, isNaN(targetAlignment) ? 0.0 : targetAlignment));

                return new Response(JSON.stringify({
                    execution_result: executionResult,
                    actionability_score: actionability,
                    constraint_adherence_score: constraintAdherence,
                    target_alignment_score: targetAlignment,
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
