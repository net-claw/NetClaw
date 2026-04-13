import type { SvgIconProps } from "./svg-icon.props"

const LogoIcon = ({ className = "size-6" }: SvgIconProps) => {
  return (
    <svg
      className={className}
      width="397"
      height="397"
      viewBox="0 0 397 397"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      <rect width="397" height="397" rx="60" fill="black" />

      <g transform="translate(30 30) scale(0.85)">
        <path
          d="M113.039 233.449C102.948 233.449 97.3513 221.729 103.676 213.843L227.437 59.555C235.475 49.5343 251.479 57.4188 248.484 69.9237L211.527 224.212C210.23 229.629 205.399 233.449 199.843 233.449H113.039Z"
          fill="white"
        />

        <path
          d="M283.961 163.551C294.052 163.551 299.649 175.271 293.324 183.157L169.564 337.445C161.526 347.466 145.522 339.581 148.517 327.076L185.473 172.788C186.771 167.37 191.602 163.551 197.157 163.551H283.961Z"
          fill="white"
        />
      </g>
    </svg>
  )
}
export default LogoIcon
