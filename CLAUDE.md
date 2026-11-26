# EI Dungeon project guidance

Follow the repository conventions in [AGENTS.md](AGENTS.md) and the applicable module instructions in [api/AGENTS.md](api/AGENTS.md) and [web/AGENTS.md](web/AGENTS.md).

## Emails to users

Always render user-facing emails with the shared EI Dungeon HTML email template. Provide a plain-text alternative with equivalent information and localize both versions in the recipient language supported by the flow. Keep delivery provider code separate from email wording and layout.

## Compose and environment changes

Whenever application Compose configuration or environment variables change, update the corresponding production configuration in `../infra/apps/dungeon/` as part of the same work. Keep the production `compose.yaml`, `.env.example`, and `DEPLOYMENT.md` aligned with the application settings, and validate the updated production configuration with `docker compose config --quiet`.
