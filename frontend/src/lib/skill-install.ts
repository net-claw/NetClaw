export type SkillInstallState = {
  status: string
  missingCommands: string[]
  requiredCommands: string[]
  lastError?: string | null
  installedAt?: string | null
}

export function parseSkillInstallState(metadataJson?: string | null): SkillInstallState | null {
  if (!metadataJson) {
    return null
  }

  try {
    const parsed = JSON.parse(metadataJson) as {
      install?: {
        status?: string
        missingCommands?: string[]
        requiredCommands?: string[]
        lastError?: string | null
        installedAt?: string | null
      }
    }

    if (!parsed.install) {
      return null
    }

    return {
      status: parsed.install.status ?? "unknown",
      missingCommands: parsed.install.missingCommands ?? [],
      requiredCommands: parsed.install.requiredCommands ?? [],
      lastError: parsed.install.lastError ?? null,
      installedAt: parsed.install.installedAt ?? null,
    }
  } catch {
    return null
  }
}

export function formatSkillInstallLabel(state: SkillInstallState | null): string {
  if (!state) {
    return "No runtime"
  }

  switch (state.status) {
    case "installed":
      return "Installed"
    case "missing":
      return "Install required"
    case "failed":
      return "Install failed"
    case "not_applicable":
      return "No runtime"
    default:
      return state.status
  }
}
