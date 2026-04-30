# AI Chat Setup

InternHub includes an AI assistant panel. The browser never receives your OpenAI API key; all AI calls go through the .NET backend.

Update `InternHub.Api/appsettings.Development.json`:

```json
"OpenAI": {
  "ApiKey": "sk-your-key-here",
  "Model": "gpt-5.4-mini"
}
```

If `ApiKey` is empty, the assistant still works in local fallback mode and can summarize basic app status from SQL Server.

The assistant can help with:

- Overdue onboarding work
- Employee onboarding status
- Email setup
- Asset and document workflow questions
- What HR or managers should do next
