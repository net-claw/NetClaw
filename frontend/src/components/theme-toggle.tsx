import { useEffect, useState } from "react"
import { AnimatePresence, motion } from "framer-motion"
import { Moon, Sun } from "lucide-react"

import { useTheme } from "@/components/theme-provider-context"

const COLOR_SCHEME_QUERY = "(prefers-color-scheme: dark)"

export function ThemeToggle() {
  const { theme, setTheme } = useTheme()
  const [systemTheme, setSystemTheme] = useState<"dark" | "light">("light")

  useEffect(() => {
    const mediaQuery = window.matchMedia(COLOR_SCHEME_QUERY)

    const syncSystemTheme = () => {
      setSystemTheme(mediaQuery.matches ? "dark" : "light")
    }

    syncSystemTheme()
    mediaQuery.addEventListener("change", syncSystemTheme)

    return () => {
      mediaQuery.removeEventListener("change", syncSystemTheme)
    }
  }, [])

  const isDark = (theme === "system" ? systemTheme : theme) === "dark"

  const toggle = () => {
    setTheme(isDark ? "light" : "dark")
  }

  return (
    <button
      type="button"
      onClick={toggle}
      className="relative h-8 w-16 rounded-full bg-linear-to-r from-amber-200 to-orange-300 p-0.5 shadow-lg transition-all duration-700 ease-in-out hover:shadow-xl focus:outline-none dark:from-indigo-900 dark:to-slate-900"
      aria-label="Toggle theme"
    >
      <AnimatePresence>
        {isDark ? (
          <motion.div
            key="stars"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.5 }}
            className="absolute inset-0 overflow-hidden rounded-full"
          >
            {[
              { top: "20%", left: "15%", size: 2, delay: 0 },
              { top: "35%", left: "25%", size: 1.5, delay: 0.1 },
              { top: "55%", left: "10%", size: 1, delay: 0.2 },
              { top: "25%", left: "40%", size: 1.5, delay: 0.15 },
              { top: "65%", left: "30%", size: 2, delay: 0.05 },
              { top: "45%", left: "18%", size: 1, delay: 0.25 },
            ].map((star, index) => (
              <motion.div
                key={index}
                className="absolute rounded-full bg-white"
                style={{
                  top: star.top,
                  left: star.left,
                  width: star.size,
                  height: star.size,
                }}
                initial={{ opacity: 0, scale: 0 }}
                animate={{ opacity: [0, 1, 0.5, 1], scale: 1 }}
                transition={{
                  duration: 2,
                  delay: star.delay,
                  repeat: Number.POSITIVE_INFINITY,
                  repeatType: "reverse",
                }}
              />
            ))}
          </motion.div>
        ) : (
          <motion.div
            key="clouds"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.4 }}
            className="absolute inset-0 overflow-hidden rounded-full"
          >
            <motion.div
              className="absolute h-2 w-4 rounded-full bg-white/50"
              style={{ top: "55%", left: "12%" }}
              animate={{ x: [0, 4, 0] }}
              transition={{
                duration: 4,
                repeat: Number.POSITIVE_INFINITY,
                ease: "easeInOut",
              }}
            />
            <motion.div
              className="absolute h-1.5 w-3 rounded-full bg-white/40"
              style={{ top: "30%", left: "30%" }}
              animate={{ x: [0, -3, 0] }}
              transition={{
                duration: 3.5,
                repeat: Number.POSITIVE_INFINITY,
                ease: "easeInOut",
              }}
            />
          </motion.div>
        )}
      </AnimatePresence>

      <motion.div
        layout
        className="relative z-10 flex h-7 w-7 items-center justify-center rounded-full shadow-md"
        animate={{
          x: isDark ? 32 : 0,
          backgroundColor: isDark ? "#1e1b4b" : "#fbbf24",
        }}
        transition={{
          type: "spring",
          stiffness: 500,
          damping: 30,
        }}
      >
        <AnimatePresence mode="wait">
          {isDark ? (
            <motion.div
              key="moon"
              initial={{ rotate: -90, scale: 0, opacity: 0 }}
              animate={{ rotate: 0, scale: 1, opacity: 1 }}
              exit={{ rotate: 90, scale: 0, opacity: 0 }}
              transition={{ duration: 0.3, ease: "easeOut" }}
            >
              <Moon className="h-4 w-4 text-indigo-200" strokeWidth={1.5} />
            </motion.div>
          ) : (
            <motion.div
              key="sun"
              initial={{ rotate: 90, scale: 0, opacity: 0 }}
              animate={{ rotate: 0, scale: 1, opacity: 1 }}
              exit={{ rotate: -90, scale: 0, opacity: 0 }}
              transition={{ duration: 0.3, ease: "easeOut" }}
            >
              <Sun className="h-4 w-4 text-amber-700" strokeWidth={1.5} />
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>
    </button>
  )
}
