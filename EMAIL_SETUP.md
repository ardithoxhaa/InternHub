# Real Email Setup

InternHub can send real emails through SMTP. It is disabled by default so local development works without secrets.

Update `InternHub.Api/appsettings.Development.json`:

```json
"Email": {
  "Enabled": true,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "UseSsl": true,
  "From": "your-email@gmail.com",
  "Username": "your-email@gmail.com",
  "Password": "your-app-password"
}
```

For Gmail, use an app password, not your normal Gmail password.

Emails are sent when:

- A user registers
- A welcome email is sent from an employee profile
- An onboarding plan is generated

If SMTP is disabled or incomplete, the app still creates in-app notifications and logs the email attempt.
