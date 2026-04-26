import { Box, IconButton, Typography, Stack } from '@mui/material'
import YardOutlinedIcon from '@mui/icons-material/YardOutlined'
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined'
import { useAuth } from '../../contexts/AuthContext'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getLatestWeather } from '../../api/weather'

function greeting(displayName: string) {
  const hour = new Date().getHours()
  const first = displayName.split(' ')[0]
  if (hour < 12) return `Good morning, ${first}`
  if (hour < 17) return `Good afternoon, ${first}`
  return `Good evening, ${first}`
}

export function AppHeader() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const { data: weather } = useQuery({
    queryKey: ['weather', user?.householdId],
    queryFn: () => getLatestWeather(user!.householdId!),
    enabled: !!user?.householdId,
    staleTime: 5 * 60 * 1000,
    refetchInterval: 10 * 60 * 1000,
  })

  return (
    <Box
      sx={{
        bgcolor: 'primary.main',
        color: '#fff',
        px: { xs: 2, sm: 3 },
        py: 1.25,
      }}
    >
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
        <Stack direction="row" sx={{ alignItems: 'center', gap: 1, flexWrap: 'nowrap', minWidth: 0 }}>
          <YardOutlinedIcon sx={{ fontSize: 20, opacity: 0.85, flexShrink: 0 }} />
          <Typography
            variant="h6"
            sx={{ fontFamily: '"Spectral", serif', fontWeight: 600, letterSpacing: 0.3, whiteSpace: 'nowrap' }}
          >
            {user ? greeting(user.displayName) : 'Garden Companion'}
          </Typography>
          {weather && (
            <Typography
              variant="body2"
              sx={{ opacity: 0.85, fontSize: '0.8rem', whiteSpace: 'nowrap', ml: 0.5 }}
            >
              · {Math.round(Number(weather.temperatureF))}°F · {Math.round(Number(weather.humidity))}% humidity
            </Typography>
          )}
        </Stack>
        {user && (
          <IconButton
            size="small"
            onClick={() => navigate('/settings')}
            sx={{ color: '#fff', opacity: 0.8, flexShrink: 0, ml: 1, '&:hover': { opacity: 1, bgcolor: 'rgba(255,255,255,0.12)' } }}
            aria-label="Settings"
          >
            <SettingsOutlinedIcon sx={{ fontSize: 20 }} />
          </IconButton>
        )}
      </Stack>
    </Box>
  )
}
