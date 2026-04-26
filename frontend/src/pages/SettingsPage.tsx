import { useState, useEffect, type FormEvent } from 'react'
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  Container,
  Divider,
  IconButton,
  Skeleton,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined'
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getUserSettings, updateUserSettings } from '../api/users'
import {
  getHousehold,
  createHousehold,
  updateHousehold,
  inviteHouseholdMember,
  removeHouseholdMember,
} from '../api/households'
import { useAuth } from '../contexts/AuthContext'
import { AppHeader } from '../components/layout/AppHeader'

// ── Profile section ───────────────────────────────────────────────────────────

function ProfileSection() {
  const { updateUser } = useAuth()
  const queryClient = useQueryClient()

  const { data: settings, isLoading } = useQuery({
    queryKey: ['user-settings'],
    queryFn: getUserSettings,
  })

  const [displayName, setDisplayName] = useState('')
  const [nameError, setNameError] = useState('')
  const [savedName, setSavedName] = useState(false)
  const [savedNotifs, setSavedNotifs] = useState(false)
  const [nameInitialized, setNameInitialized] = useState(false)

  useEffect(() => {
    if (settings && !nameInitialized) {
      setDisplayName(settings.displayName)
      setNameInitialized(true)
    }
  }, [settings, nameInitialized])

  const nameMutation = useMutation({
    mutationFn: (name: string) => updateUserSettings({ displayName: name }),
    onSuccess: (data) => {
      updateUser({ displayName: data.displayName })
      queryClient.setQueryData(['user-settings'], data)
      setSavedName(true)
      setTimeout(() => setSavedName(false), 2000)
    },
  })

  const notifMutation = useMutation({
    mutationFn: (enabled: boolean) => updateUserSettings({ emailNotificationsEnabled: enabled }),
    onSuccess: (data) => {
      queryClient.setQueryData(['user-settings'], data)
      setSavedNotifs(true)
      setTimeout(() => setSavedNotifs(false), 2000)
    },
  })

  function handleNameSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = displayName.trim()
    if (!trimmed) {
      setNameError('Display name is required.')
      return
    }
    setNameError('')
    nameMutation.mutate(trimmed)
  }

  if (isLoading) {
    return (
      <Card sx={{ p: 3 }}>
        <Skeleton variant="text" width="30%" height={32} sx={{ mb: 2 }} />
        <Skeleton variant="rectangular" height={56} sx={{ borderRadius: 1.5, mb: 2 }} />
        <Skeleton variant="rectangular" height={56} sx={{ borderRadius: 1.5 }} />
      </Card>
    )
  }

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ alignItems: 'center', gap: 1, mb: 3 }}>
        <PersonOutlineOutlinedIcon sx={{ color: 'primary.main', fontSize: 20 }} />
        <Typography variant="h6">Your Profile</Typography>
      </Stack>

      <form onSubmit={handleNameSubmit} noValidate>
        <Stack sx={{ gap: 2 }}>
          <Stack direction="row" sx={{ gap: 1.5, alignItems: 'flex-start' }}>
            <TextField
              label="Display name"
              value={displayName}
              onChange={e => { setDisplayName(e.target.value); setSavedName(false) }}
              error={!!nameError}
              helperText={nameError}
              size="small"
              sx={{ flex: 1 }}
            />
            <Button
              type="submit"
              variant="contained"
              color="secondary"
              size="small"
              disabled={nameMutation.isPending}
              sx={{ mt: 0.5, flexShrink: 0 }}
            >
              {savedName ? 'Saved!' : nameMutation.isPending ? 'Saving…' : 'Save'}
            </Button>
          </Stack>

          {nameMutation.isError && (
            <Alert severity="error" sx={{ py: 0.5 }}>
              Could not save display name. Please try again.
            </Alert>
          )}

          <Divider />

          <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
            <Box>
              <Typography variant="body2">Email notifications</Typography>
              <Typography variant="caption" color="text.secondary">
                Receive reminders about overdue tasks and upcoming harvests
              </Typography>
            </Box>
            <Stack direction="row" sx={{ alignItems: 'center', gap: 1 }}>
              {savedNotifs && (
                <Typography variant="caption" color="primary.main">Saved!</Typography>
              )}
              <Switch
                checked={settings?.emailNotificationsEnabled ?? false}
                onChange={e => notifMutation.mutate(e.target.checked)}
                disabled={notifMutation.isPending}
                color="primary"
              />
            </Stack>
          </Stack>
        </Stack>
      </form>
    </Card>
  )
}

// ── Household section ─────────────────────────────────────────────────────────

function CreateHouseholdCard({ onCreated }: { onCreated: (id: string) => void }) {
  const [name, setName] = useState('')
  const [nameError, setNameError] = useState('')

  const mutation = useMutation({
    mutationFn: createHousehold,
    onSuccess: (household) => onCreated(household.id),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) {
      setNameError('Household name is required.')
      return
    }
    setNameError('')
    mutation.mutate(trimmed)
  }

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ alignItems: 'center', gap: 1, mb: 1 }}>
        <HomeOutlinedIcon sx={{ color: 'primary.main', fontSize: 20 }} />
        <Typography variant="h6">Household</Typography>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Create a household to share gardens with family members or housemates.
      </Typography>
      <form onSubmit={handleSubmit} noValidate>
        <Stack direction="row" sx={{ gap: 1.5, alignItems: 'flex-start' }}>
          <TextField
            label="Household name"
            value={name}
            onChange={e => setName(e.target.value)}
            error={!!nameError}
            helperText={nameError}
            size="small"
            autoFocus
            sx={{ flex: 1 }}
          />
          <Button
            type="submit"
            variant="contained"
            color="secondary"
            size="small"
            disabled={mutation.isPending}
            sx={{ mt: 0.5, flexShrink: 0 }}
          >
            {mutation.isPending ? 'Creating…' : 'Create'}
          </Button>
        </Stack>
        {mutation.isError && (
          <Alert severity="error" sx={{ mt: 1.5, py: 0.5 }}>
            Could not create household. Please try again.
          </Alert>
        )}
      </form>
    </Card>
  )
}

function HouseholdSection({ householdId }: { householdId: string }) {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  const { data: household, isLoading, isError } = useQuery({
    queryKey: ['household', householdId],
    queryFn: () => getHousehold(householdId),
  })

  const [householdName, setHouseholdName] = useState('')
  const [nameError, setNameError] = useState('')
  const [savedName, setSavedName] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteError, setInviteError] = useState('')
  const [nameInitialized, setNameInitialized] = useState(false)

  useEffect(() => {
    if (household && !nameInitialized) {
      setHouseholdName(household.name)
      setNameInitialized(true)
    }
  }, [household, nameInitialized])

  const isOwner = household?.members.some(
    m => m.userId === user?.userId && m.role === 'Owner'
  ) ?? false

  const updateNameMutation = useMutation({
    mutationFn: (name: string) => updateHousehold(householdId, { name }),
    onSuccess: (data) => {
      queryClient.setQueryData(['household', householdId], data)
      setSavedName(true)
      setTimeout(() => setSavedName(false), 2000)
    },
  })

  const inviteMutation = useMutation({
    mutationFn: (email: string) => inviteHouseholdMember(householdId, email),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['household', householdId] })
      setInviteEmail('')
      setInviteError('')
    },
    onError: () => {
      setInviteEmail('')
      setInviteError('Could not invite this user. Make sure the email is correct and they have an account.')
    },
  })

  const removeMutation = useMutation({
    mutationFn: (userId: string) => removeHouseholdMember(householdId, userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['household', householdId] }),
  })

  function handleNameSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = householdName.trim()
    if (!trimmed) {
      setNameError('Household name is required.')
      return
    }
    setNameError('')
    updateNameMutation.mutate(trimmed)
  }

  function handleInvite(e: FormEvent) {
    e.preventDefault()
    const trimmed = inviteEmail.trim()
    if (!trimmed) return
    setInviteError('')
    inviteMutation.mutate(trimmed)
  }

  if (isLoading) {
    return (
      <Card sx={{ p: 3 }}>
        <Skeleton variant="text" width="30%" height={32} sx={{ mb: 2 }} />
        <Skeleton variant="rectangular" height={56} sx={{ borderRadius: 1.5, mb: 3 }} />
        <Skeleton variant="text" width="20%" height={24} sx={{ mb: 1 }} />
        {[0, 1].map(i => (
          <Stack key={i} direction="row" sx={{ alignItems: 'center', py: 1.5, gap: 1.5 }}>
            <Skeleton variant="circular" width={32} height={32} />
            <Skeleton variant="text" width="40%" />
          </Stack>
        ))}
      </Card>
    )
  }

  if (isError) {
    return (
      <Card sx={{ p: 3 }}>
        <Alert severity="error">Could not load household.</Alert>
      </Card>
    )
  }

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ alignItems: 'center', gap: 1, mb: 3 }}>
        <HomeOutlinedIcon sx={{ color: 'primary.main', fontSize: 20 }} />
        <Typography variant="h6">Household</Typography>
      </Stack>

      {/* Household name */}
      {isOwner && (
        <form onSubmit={handleNameSubmit} noValidate>
          <Stack direction="row" sx={{ gap: 1.5, alignItems: 'flex-start', mb: 3 }}>
            <TextField
              label="Household name"
              value={householdName}
              onChange={e => { setHouseholdName(e.target.value); setSavedName(false) }}
              error={!!nameError}
              helperText={nameError}
              size="small"
              sx={{ flex: 1 }}
            />
            <Button
              type="submit"
              variant="outlined"
              color="primary"
              size="small"
              disabled={updateNameMutation.isPending}
              sx={{ mt: 0.5, flexShrink: 0 }}
            >
              {savedName ? 'Saved!' : updateNameMutation.isPending ? 'Saving…' : 'Rename'}
            </Button>
          </Stack>
        </form>
      )}
      {!isOwner && (
        <Typography variant="body1" sx={{ mb: 3, fontWeight: 500 }}>
          {household?.name}
        </Typography>
      )}

      {/* Member list */}
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1, fontWeight: 500, textTransform: 'uppercase', letterSpacing: 0.5, fontSize: '0.7rem' }}>
        Members
      </Typography>
      <Card sx={{ p: 0, overflow: 'hidden', mb: isOwner ? 3 : 0 }}>
        {household?.members.map((member, idx) => (
          <Box key={member.userId}>
            {idx > 0 && <Divider />}
            <Stack direction="row" sx={{ alignItems: 'center', px: 2, py: 1.5, gap: 1.5 }}>
              <Box
                sx={{
                  width: 32, height: 32, borderRadius: '50%',
                  bgcolor: 'primary.main', display: 'flex',
                  alignItems: 'center', justifyContent: 'center', flexShrink: 0,
                }}
              >
                <Typography variant="caption" sx={{ color: '#fff', fontWeight: 600 }}>
                  {member.displayName[0].toUpperCase()}
                </Typography>
              </Box>
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="body2">{member.displayName}</Typography>
                <Typography variant="caption" color="text.secondary">{member.email}</Typography>
              </Box>
              <Chip label={member.role} size="small" sx={{ fontSize: '0.7rem' }} />
              {isOwner && member.userId !== user?.userId && (
                <IconButton
                  size="small"
                  onClick={() => removeMutation.mutate(member.userId)}
                  disabled={removeMutation.isPending}
                  sx={{ color: 'text.secondary' }}
                  aria-label={`Remove ${member.displayName}`}
                >
                  <DeleteOutlineOutlinedIcon sx={{ fontSize: 16 }} />
                </IconButton>
              )}
            </Stack>
          </Box>
        ))}
      </Card>

      {/* Invite form (owner only) */}
      {isOwner && (
        <form onSubmit={handleInvite} noValidate>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1, fontWeight: 500, textTransform: 'uppercase', letterSpacing: 0.5, fontSize: '0.7rem' }}>
            Invite member
          </Typography>
          <Stack direction="row" sx={{ gap: 1.5, alignItems: 'flex-start' }}>
            <TextField
              label="Email address"
              type="email"
              value={inviteEmail}
              onChange={e => { setInviteEmail(e.target.value); setInviteError('') }}
              size="small"
              sx={{ flex: 1 }}
            />
            <Button
              type="submit"
              variant="contained"
              color="secondary"
              size="small"
              disabled={inviteMutation.isPending || !inviteEmail.trim()}
              startIcon={<AddOutlinedIcon />}
              sx={{ mt: 0.5, flexShrink: 0 }}
            >
              {inviteMutation.isPending ? 'Inviting…' : 'Invite'}
            </Button>
          </Stack>
          {inviteError && (
            <Alert severity="error" sx={{ mt: 1.5, py: 0.5 }}>
              {inviteError}
            </Alert>
          )}
        </form>
      )}
    </Card>
  )
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function SettingsPage() {
  const navigate = useNavigate()
  const { user, updateUser } = useAuth()

  function handleHouseholdCreated(id: string) {
    updateUser({ householdId: id })
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppHeader />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(-1)}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back
        </Button>

        <Typography variant="h4" sx={{ mb: 4 }}>
          Settings
        </Typography>

        <Stack sx={{ gap: 3 }}>
          <ProfileSection />

          {user?.householdId ? (
            <HouseholdSection householdId={user.householdId} />
          ) : (
            <CreateHouseholdCard onCreated={handleHouseholdCreated} />
          )}
        </Stack>
      </Container>
    </Box>
  )
}
