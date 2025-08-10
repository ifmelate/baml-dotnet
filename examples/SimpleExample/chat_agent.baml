// Example BAML schema for a simple chat agent
class Message {
    role string
    content string
}

class ReplyTool {
    response string
}

class StopTool {
    action "stop" @description("when it might be a good time to end the conversation")
}

function ChatAgent(messages: Message[], tone: "happy" | "sad") -> ReplyTool | StopTool {
    client "openai/gpt-4o-mini"
    
    prompt #"
        Be a {{ tone }} bot.

        {{ ctx.output_format }}

        {% for m in messages %}
        {{ _.role(m.role) }}
        {{ m.content }}
        {% endfor %}
    "#
}

function ExtractInfo(text: string) -> string {
    client "openai/gpt-4o-mini"
    
    prompt #"
        Extract the key information from the following text:
        {{ text }}
        
        Return only the extracted information as a string.
    "#
}

