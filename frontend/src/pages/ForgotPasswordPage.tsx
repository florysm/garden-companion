import { useState, type FormEvent } from 'react'
import {
  Box,
  Button,
  Card,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import YardOutlinedIcon from '@mui/icons-material/YardOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { authApi } from '../api/auth'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [sent, setSent] = useState(false)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    try {
      await authApi.forgotPassword(email)
    } finally {
      setSent(true)
      setLoading(false)
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        bgcolor: 'background.default',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        px: 2,
      }}
    >
      <Stack sx={{ alignItems: 'center', mb: 4, gap: 1 }}>
        <YardOutlinedIcon sx={{ fontSize: 36, color: 'primary.main' }} />
        <Typography variant="h3" sx={{ color: 'text.primary' }}>
          Garden Companion
        </Typography>
      </Stack>

      <Card sx={{ width: '100%', maxWidth: 400, p: 4 }}>
        {sent ? (
          <Stack sx={{ gap: 2, alignItems: 'flex-start' }}>
            <Typography variant="h5">Check your inbox</Typography>
            <Typography variant="body2" color="text.secondary">
              If an account exists for <strong>{email}</strong>, we sent a reset link. It may take a
              minute to arrive.
            </Typography>
            <Link component={RouterLink} to="/login" variant="body2" color="primary">
              Back to sign in
            </Link>
          </Stack>
        ) : (
          <>
            <Typography variant="h5" sx={{ mb: 1 }}>
              Reset your password
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Enter your email and we&apos;ll send a reset link.
            </Typography>

            <form onSubmit={handleSubmit} noValidate>
              <Stack sx={{ gap: 2.5 }}>
                <TextField
                  label="Email"
                  type="email"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  autoComplete="email"
                  autoFocus
                  required
                />
                <Button
                  type="submit"
                  variant="contained"
                  color="secondary"
                  size="large"
                  disabled={loading}
                >
                  {loading ? 'Sending…' : 'Send reset link'}
                </Button>
              </Stack>
            </form>

            <Stack sx={{ mt: 3, alignItems: 'center' }}>
              <Link component={RouterLink} to="/login" variant="body2" color="text.secondary">
                Back to sign in
              </Link>
            </Stack>
          </>
        )}
      </Card>
    </Box>
  )
}
