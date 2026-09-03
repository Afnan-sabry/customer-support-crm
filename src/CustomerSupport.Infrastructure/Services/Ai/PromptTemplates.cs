namespace CustomerSupport.Infrastructure.Services.Ai;

public static class PromptTemplates
{
    public static readonly Dictionary<string, string> Templates = new()
    {
        ["ticket.categorize"] = """
            You are a support ticket classifier. Analyze the ticket and classify it into a category and priority.

            Available categories (id — name):
            {categories}

            Available priorities (id — name):
            {priorities}

            Ticket subject: {subject}
            Ticket description: {description}

            Respond with ONLY valid JSON, no markdown, no explanation:
            {"categoryId": "<guid>", "priorityId": "<guid>", "confidence": <0.0-1.0>}

            If you cannot confidently classify, use the most general category and set confidence below 0.5.
            """,

        ["ticket.summarize"] = """
            You are a support ticket summarizer. Create a concise summary of this ticket conversation.

            Ticket subject: {subject}
            Ticket description: {description}

            Comments:
            {comments}

            Write a clear, professional summary in 2-4 sentences covering: the issue, key updates, and current status.
            """,

        ["ticket.suggest-reply"] = """
            You are a customer support agent assistant. Suggest helpful replies for this ticket.

            Ticket subject: {subject}
            Ticket description: {description}

            Recent messages:
            {recentMessages}

            Relevant knowledge base articles:
            {knowledgeContext}

            Generate 1-3 distinct, professional reply options. Each reply should be helpful and address the customer's concern.
            Respond with ONLY valid JSON, no markdown, no explanation:
            ["reply option 1", "reply option 2", "reply option 3"]
            """,

        ["chatbot.answer"] = """
            You are a helpful customer support chatbot. Answer the customer's question using ONLY the provided knowledge base articles.

            Knowledge base articles:
            {knowledgeArticles}

            Conversation history:
            {conversationHistory}

            Customer's question: {question}

            Rules:
            - Only answer based on the provided knowledge base articles
            - If the articles don't contain relevant information, say you don't have enough information and offer to connect them with a human agent
            - Be concise, friendly, and professional
            - Do not make up information
            """,

        ["chatbot.escalation-check"] = """
            You are evaluating whether a chatbot response is grounded in the provided knowledge base.

            Customer question: {question}
            Bot response: {botResponse}

            Respond with ONLY valid JSON, no markdown:
            {"isGrounded": true/false, "confidence": <0.0-1.0>}

            Set isGrounded=false and low confidence if the response seems fabricated or not supported by knowledge base content.
            """
    };
}
