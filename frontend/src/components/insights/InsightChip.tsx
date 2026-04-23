import { Chip } from '@mui/material'
import type { SvgIconComponent } from '@mui/icons-material'
import BugReportOutlinedIcon from '@mui/icons-material/BugReportOutlined'
import WbSunnyOutlinedIcon from '@mui/icons-material/WbSunnyOutlined'
import LoopOutlinedIcon from '@mui/icons-material/LoopOutlined'
import TrendingUpOutlinedIcon from '@mui/icons-material/TrendingUpOutlined'
import YardOutlinedIcon from '@mui/icons-material/YardOutlined'
import WaterDropOutlinedIcon from '@mui/icons-material/WaterDropOutlined'
import type { UserInsight } from '../../api/insights'

interface TypeConfig {
  Icon: SvgIconComponent
  chipBg: string
  chipColor: string
}

const TYPE_CONFIG: Record<string, TypeConfig> = {
  PestWarning:            { Icon: BugReportOutlinedIcon,  chipBg: '#FDECEA', chipColor: '#B85C4A' },
  WeatherAlert:           { Icon: WbSunnyOutlinedIcon,    chipBg: '#FBF0EB', chipColor: '#C4714A' },
  RotationWarning:        { Icon: LoopOutlinedIcon,       chipBg: '#F5F0E0', chipColor: '#8B6914' },
  YieldTrend:             { Icon: TrendingUpOutlinedIcon, chipBg: '#EEF1EC', chipColor: '#6B7F5E' },
  PlantingRecommendation: { Icon: YardOutlinedIcon,       chipBg: '#EEF1EC', chipColor: '#6B7F5E' },
  WateringRecommendation: { Icon: WaterDropOutlinedIcon,  chipBg: '#E8F0F5', chipColor: '#3D6B7A' },
}

const DEFAULT_CONFIG: TypeConfig = {
  Icon: YardOutlinedIcon,
  chipBg: '#EEF1EC',
  chipColor: '#6B7F5E',
}

export function InsightChip({
  insight,
  onDismiss,
}: {
  insight: UserInsight
  onDismiss: () => void
}) {
  const cfg = TYPE_CONFIG[insight.insightType] ?? DEFAULT_CONFIG
  const { Icon } = cfg

  return (
    <Chip
      icon={<Icon sx={{ color: `${cfg.chipColor} !important`, fontSize: '16px !important' }} />}
      label={insight.title}
      onDelete={onDismiss}
      sx={{
        bgcolor: cfg.chipBg,
        color: cfg.chipColor,
        fontWeight: 500,
        fontSize: '0.8rem',
        height: 'auto',
        py: 0.75,
        flexShrink: 0,
        '& .MuiChip-deleteIcon': {
          color: cfg.chipColor,
          opacity: 0.5,
          '&:hover': { opacity: 1, color: cfg.chipColor },
        },
      }}
    />
  )
}
