import { Routes, Route, Navigate } from 'react-router-dom'
import { Box, Typography } from '@mui/material'

// Placeholder pages — implement each slice as we build features
function Dashboard() {
  return (
    <Box sx={{ p: 4 }}>
      <Typography variant="h4">Garden Companion</Typography>
      <Typography variant="body1" color="text.secondary">
        Dashboard coming soon.
      </Typography>
    </Box>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Dashboard />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
