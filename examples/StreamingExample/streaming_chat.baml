// Example BAML schema for streaming chat
class Message {
    role string
    content string
}

class StreamingResponse {
    content string
    finished bool
}

function StreamingChat(messages: Message[], topic: string) -> StreamingResponse {
    client "openai/gpt-4o-mini"
    
    prompt #"
        You are a helpful assistant discussing {{ topic }}.
        
        {{ ctx.output_format }}

        {% for m in messages %}
        {{ _.role(m.role) }}
        {{ m.content }}
        {% endfor %}
    "#
}

function GenerateStory(prompt: string, length: "short" | "medium" | "long") -> string {
    client "openai/gpt-4o-mini"
    
    prompt #"
        Write a {{ length }} story based on this prompt: {{ prompt }}
        
        Make it engaging and creative.
    "#
}

