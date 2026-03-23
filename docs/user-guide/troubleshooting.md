# Troubleshooting

## Common Issues

### Session Expired

**Symptom:** You are redirected to the login page unexpectedly.

**Solution:** Log in again. Sessions expire after a period of inactivity for security reasons. Your access token is stored per browser tab and is cleared when you close the tab.

### Permission Denied

**Symptom:** You see an "Unauthorized" page or cannot access certain features.

**Solution:**
1. Verify you have the correct role assigned for the feature you're trying to access.
2. Contact your Platform Admin to review your permissions.
3. If you need temporary elevated access, use the **JIT Access** request feature.

### Data Not Loading

**Symptom:** Pages show loading spinners indefinitely or display error states.

**Solution:**
1. Check your internet connection.
2. Refresh the page (F5 or Ctrl+R).
3. Clear your browser cache and try again.
4. If the issue persists, the backend service may be temporarily unavailable — wait a few minutes and retry.

### MFA Issues

**Symptom:** Unable to complete multi-factor authentication.

**Solution:**
1. Ensure your authenticator app is synchronized with the correct time.
2. If you've lost access to your MFA device, contact your Platform Admin for a reset.

### Search Not Returning Expected Results

**Symptom:** Global search doesn't find a service or contract you know exists.

**Solution:**
1. Check your spelling and try alternative terms.
2. Ensure you have permission to view the resource.
3. The resource may be in a different tenant — verify your tenant selection.

## Error Messages

| Error | Meaning | Action |
|-------|---------|--------|
| `401 Unauthorized` | Session expired or invalid credentials | Log in again |
| `403 Forbidden` | Insufficient permissions | Contact Platform Admin |
| `404 Not Found` | Resource doesn't exist or was deleted | Verify the URL or resource ID |
| `500 Internal Error` | Server-side issue | Retry; report if persistent |

## Getting Help

- **Platform Admin** — For access, permissions, and configuration issues
- **Team Lead** — For workflow and process questions
- **Documentation** — Check this user guide and the operational runbooks
- **Support** — Contact your organization's NexTraceOne support channel
