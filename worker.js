export default {
    async fetch(request, env) {
        const url = new URL(request.url);

        // Intercept AI API calls
        if (url.pathname === "/api/generate" && request.method === "POST") {
            try {
                const body = await request.json();
                const systemPrompt = body.systemPrompt || "You are a helpful assistant.";
                const userPrompt = body.userPrompt || "";

                const messages = [
                    { role: "system", content: systemPrompt },
                    { role: "user", content: userPrompt }
                ];

                const response = await env.AI.run("@cf/meta/llama-3-8b-instruct", { messages });
                
                return new Response(JSON.stringify({ result: response.response }), {
                    headers: { "Content-Type": "application/json" }
                });
            } catch (err) {
                return new Response(JSON.stringify({ error: err.message }), { 
                    status: 500,
                    headers: { "Content-Type": "application/json" }
                });
            }
        }

        // Fallback to serving the Blazor WASM static assets
        return env.ASSETS.fetch(request);
    }
};
