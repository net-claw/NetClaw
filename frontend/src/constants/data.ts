export const providerData = [
  {
    image: "/images/providers/openai.png",
    value: "openai",
    label: "ChatGPT / OpenAI",
    models: ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo"],
  },
  {
    image: "/images/providers/deepseek.png",
    value: "deepseek",
    label: "DeepSeek",
    models: ["deepseek-chat", "deepseek-reasoner"],
  },
  {
    image: "/images/providers/gemini.png",
    value: "gemini",
    label: "Gemini",
    models: [
      "gemini-3-flash-preview",
      "gemini-2.0-flash",
      "gemini-2.0-flash-lite",
      "gemini-1.5-pro",
      "gemini-1.5-flash",
    ],
  },
  {
    image: "/images/providers/mistral.png",
    value: "mistral",
    label: "Mistral",
    models: ["mistral-medium-latest"],
  },
  {
    image: "/images/providers/xai.png",
    value: "xai",
    label: "xAI",
    models: ["grok-4-1-fast-non-reasoning"],
  },
  {
    image: "/images/providers/groq.png",
    value: "groq",
    label: "Groq",
    models: ["openai/gpt-oss-120b"],
  },

  {
    image: "/images/providers/anthropic.png",
    value: "anthropic",
    label: "Anthropic",
    models: ["claude-sonnet-4-6"],
  },
  {
    image: "/images/providers/openrouter.png",
    value: "openrouter",
    label: "OpenRouter",
    models: ["minimax/minimax-m2.5:free"],
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
