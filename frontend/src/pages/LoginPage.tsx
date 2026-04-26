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
import axios from 'axios'
import { Link as RouterLink, useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: Location })?.from?.pathname ?? '/'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(email, password)
      navigate(from, { replace: true })
    } catch (err) {
      if (axios.isAxiosError(err) && !err.response) {
        setError('Unable to reach the API. Check that the debug session is still running.')
      } else {
        setError('Invalid email or password.')
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
          Gardenwise
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Learn your garden, one season at a time.
        </Typography>
      </Stack>

      <Card sx={{ width: '100%', maxWidth: 400, p: 4 }}>
        <Typography variant="h5" sx={{ mb: 3 }}>
          Sign in
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
            <TextField
              label="Password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />

            {error && (
              <Typography variant="body2" color="error">
                {error}
              </Typography>
            )}

            <Button
              type="submit"
              variant="contained"
              color="secondary"
              size="large"
              disabled={loading}
              sx={{ mt: 0.5 }}
            >
              {loading ? 'Signing in…' : 'Sign in'}
            </Button>
          </Stack>
        </form>

        <Stack sx={{ mt: 3, gap: 1, alignItems: 'center' }}>
          <Link component={RouterLink} to="/forgot-password" variant="body2" color="text.secondary">
            Forgot your password?
          </Link>
          <Typography variant="body2" color="text.secondary">
            New here?{' '}
            <Link component={RouterLink} to="/register" color="primary">
              Create an account
            </Link>
          </Typography>
        </Stack>
      </Card>
    </Box>
  )
}
