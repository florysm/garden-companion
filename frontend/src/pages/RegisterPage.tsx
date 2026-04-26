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
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(false)

  function validate() {
    const e: Record<string, string> = {}
    if (!displayName.trim()) e.displayName = 'Name is required.'
    if (!email.trim()) e.email = 'Email is required.'
    if (password.length < 8) e.password = 'Password must be at least 8 characters.'
    return e
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const fieldErrors = validate()
    if (Object.keys(fieldErrors).length > 0) {
      setErrors(fieldErrors)
      return
    }
    setErrors({})
    setLoading(true)
    try {
      await register(email, password, displayName)
      navigate('/', { replace: true })
    } catch {
      setErrors({ form: 'Could not create account. That email may already be in use.' })
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
          Create an account
        </Typography>

        <form onSubmit={handleSubmit} noValidate>
          <Stack sx={{ gap: 2.5 }}>
            <TextField
              label="Your name"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              autoComplete="name"
              autoFocus
              required
              error={!!errors.displayName}
              helperText={errors.displayName}
            />
            <TextField
              label="Email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              autoComplete="email"
              required
              error={!!errors.email}
              helperText={errors.email}
            />
            <TextField
              label="Password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="new-password"
              required
              error={!!errors.password}
              helperText={errors.password ?? 'At least 8 characters.'}
            />

            {errors.form && (
              <Typography variant="body2" color="error">
                {errors.form}
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
              {loading ? 'Creating account…' : 'Create account'}
            </Button>
          </Stack>
        </form>

        <Stack sx={{ mt: 3, alignItems: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            Already have an account?{' '}
            <Link component={RouterLink} to="/login" color="primary">
              Sign in
            </Link>
          </Typography>
        </Stack>
      </Card>
    </Box>
  )
}
