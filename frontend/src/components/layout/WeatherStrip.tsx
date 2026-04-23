import { Box, Typography, Stack } from '@mui/material'
import YardOutlinedIcon from '@mui/icons-material/YardOutlined'
import { useAuth } from '../../contexts/AuthContext'

function greeting(displayName: string) {
  const hour = new Date().getHours()
  const first = displayName.split(' ')[0]
  if (hour < 12) return `Good morning, ${first}`
  if (hour < 17) return `Good afternoon, ${first}`
  return `Good evening, ${first}`
}

export function WeatherStrip() {
  const { user } = useAuth()

  return (
    <Box
      sx={{
        bgcolor: 'primary.main',
        color: '#fff',
        px: { xs: 2, sm: 3 },
        py: 1.25,
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <YardOutlinedIcon sx={{ fontSize: 20, opacity: 0.85 }} />
        <Typography
          variant="h6"
          sx={{ fontFamily: '"Spectral", serif', fontWeight: 600, letterSpacing: 0.3 }}
        >
          {user ? greeting(user.displayName) : 'Garden Companion'}
        </Typography>
      </Stack>
    </Box>
  )
}
