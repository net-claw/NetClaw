export const providerData = [
  {
    value: "openai",
    label: "ChatGPT / OpenAI",
    models: ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo"],
  },
  {
    value: "deepseek",
    label: "DeepSeek",
    models: ["deepseek-chat", "deepseek-reasoner"],
  },
  {
    value: "gemini",
    label: "Gemini",
    models: [
      "gemini-2.0-flash",
      "gemini-2.0-flash-lite",
      "gemini-1.5-pro",
      "gemini-1.5-flash",
    ],
  },
]

export const channelData = [
  {
    label: "Telegram",
    value: "telegram",
    image: "/images/channels/telegram.png",
  },
  {
    label: "Discord",
    value: "discord",
    image: "/images/channels/discord.png",
  },
  // {
  //   label: "WhatsApp",
  //   value: "whatsapp",
  //   image: "/images/channels/whatsapp.png",
  // },
  {
    label: "Slack",
    value: "slack",
    image: "/images/channels/slack.png",
  },
  {
    label: "Web",
    value: "web",
    image: "/images/channels/web.png",
  },
]
