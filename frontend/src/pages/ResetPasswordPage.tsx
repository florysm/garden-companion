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
import { useSearchParams } from 'react-router-dom'
import { authApi } from '../api/auth'

type PageState = 'form' | 'success' | 'invalid-link'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [passwordError, setPasswordError] = useState('')
  const [confirmError, setConfirmError] = useState('')
  const [serverError, setServerError] = useState('')
  const [loading, setLoading] = useState(false)
  const [pageState, setPageState] = useState<PageState>(token ? 'form' : 'invalid-link')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()

    let valid = true
    if (password.length < 8) {
      setPasswordError('Password must be at least 8 characters.')
      valid = false
    } else {
      setPasswordError('')
    }
    if (password !== confirm) {
      setConfirmError('Passwords do not match.')
      valid = false
    } else {
      setConfirmError('')
    }
    if (!valid) return

    setServerError('')
    setLoading(true)
    try {
      await authApi.resetPassword(token, password)
      setPageState('success')
    } catch (err: any) {
      const msg: string = err?.response?.data?.error ?? ''
      if (err?.response?.status === 400 && msg.toLowerCase().includes('invalid')) {
        setPageState('invalid-link')
      } else {
        setServerError('Something went wrong. Please try again.')
      }
    } finally {
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
        {pageState === 'invalid-link' && (
          <Stack sx={{ gap: 2 }}>
            <Typography variant="h5">Link expired</Typography>
            <Typography variant="body2" color="text.secondary">
              This reset link is invalid, has expired, or has already been used. Reset links are valid
              for 1 hour.
            </Typography>
            <Link component={RouterLink} to="/forgot-password" variant="body2" color="primary">
              Request a new link
            </Link>
          </Stack>
        )}

        {pageState === 'success' && (
          <Stack sx={{ gap: 2 }}>
            <Typography variant="h5">Password updated</Typography>
            <Typography variant="body2" color="text.secondary">
              Your password has been reset. You can now sign in with your new password.
            </Typography>
            <Link component={RouterLink} to="/login" variant="body2" color="primary">
              Go to sign in
            </Link>
          </Stack>
        )}

        {pageState === 'form' && (
          <>
            <Typography variant="h5" sx={{ mb: 1 }}>
              Choose a new password
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Must be at least 8 characters.
            </Typography>

            <form onSubmit={handleSubmit} noValidate>
              <Stack sx={{ gap: 2.5 }}>
                <TextField
                  label="New password"
                  type="password"
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  autoComplete="new-password"
                  autoFocus
                  required
                  error={!!passwordError}
                  helperText={passwordError}
                />
                <TextField
                  label="Confirm password"
                  type="password"
                  value={confirm}
                  onChange={e => setConfirm(e.target.value)}
                  autoComplete="new-password"
                  required
                  error={!!confirmError}
                  helperText={confirmError}
                />

                {serverError && (
                  <Typography variant="body2" color="error">
                    {serverError}
                  </Typography>
                )}

                <Button
                  type="submit"
                  variant="contained"
                  color="secondary"
                  size="large"
                  disabled={loading}
                >
                  {loading ? 'Saving…' : 'Set new password'}
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
