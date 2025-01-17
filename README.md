# Notify Service

A Notify Service designed to handle HTTP requests to send emails via SendGrid, and manage webhook notifications from SendGrid to publish events to AWS SNS.

## Features

- Receive HTTP requests to send emails via SendGrid.
- Receive webhook notifications from SendGrid for email events (e.g., delivery, bounce).
- Publish event data to AWS SNS for further processing.

![Diagram](NotifyService.drawio.png)

## Author
- Dao Lam