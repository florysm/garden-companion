import { useState, type FormEvent } from 'react'
import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createGarden, getGardenTypes } from '../api/gardens'
import { useAuth } from '../contexts/AuthContext'
import { WeatherStrip } from '../components/layout/WeatherStrip'

export function CreateGardenPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const queryClient = useQueryClient()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedTypeIds, setSelectedTypeIds] = useState<number[]>([])
  const [nameError, setNameError] = useState('')

  const { data: gardenTypes = [] } = useQuery({
    queryKey: ['garden-types'],
    queryFn: getGardenTypes,
  })

  const mutation = useMutation({
    mutationFn: createGarden,
    onSuccess: (garden) => {
      queryClient.invalidateQueries({ queryKey: ['gardens'] })
      navigate(`/gardens/${garden.id}`)
    },
  })

  function toggleType(id: number) {
    setSelectedTypeIds(prev =>
      prev.includes(id) ? prev.filter(t => t !== id) : [...prev, id]
    )
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) {
      setNameError('Garden name is required.')
      return
    }
    if (!user?.householdId) return
    setNameError('')
    mutation.mutate({
      householdId: user.householdId,
      name: name.trim(),
      description: description.trim() || null,
      gardenTypeIds: selectedTypeIds,
    })
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <WeatherStrip />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate('/')}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back to dashboard
        </Button>

        <Typography variant="h4" sx={{ mb: 4 }}>
          New Garden
        </Typography>

        <Card sx={{ p: 4 }}>
          <form onSubmit={handleSubmit} noValidate>
            <Stack sx={{ gap: 3 }}>
              <TextField
                label="Garden name"
                value={name}
                onChange={e => setName(e.target.value)}
                autoFocus
                required
                error={!!nameError}
                helperText={nameError}
              />

              <TextField
                label="Description"
                value={description}
                onChange={e => setDescription(e.target.value)}
                multiline
                rows={3}
                helperText="Optional — what do you grow here?"
              />

              {gardenTypes.length > 0 && (
                <Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                    Garden type
                  </Typography>
                  <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
                    {gardenTypes.map(type => {
                      const selected = selectedTypeIds.includes(type.id)
                      return (
                        <Chip
                          key={type.id}
                          label={type.name}
                          onClick={() => toggleType(type.id)}
                          sx={{
                            bgcolor: selected ? 'primary.main' : 'background.default',
                            color: selected ? '#fff' : 'text.secondary',
                            border: '1.5px solid',
                            borderColor: selected ? 'primary.main' : '#C8C5BE',
                            fontWeight: selected ? 500 : 400,
                            '&:hover': {
                              bgcolor: selected ? 'primary.dark' : '#E8E5DE',
                            },
                          }}
                        />
                      )
                    })}
                  </Stack>
                </Box>
              )}

              {mutation.isError && (
                <Typography variant="body2" color="error">
                  Something went wrong. Please try again.
                </Typography>
              )}

              <Stack direction="row" sx={{ gap: 2, justifyContent: 'flex-end', mt: 1 }}>
                <Button onClick={() => navigate('/')} color="inherit">
                  Cancel
                </Button>
                <Button
                  type="submit"
                  variant="contained"
                  color="secondary"
                  disabled={mutation.isPending}
                >
                  {mutation.isPending ? 'Creating…' : 'Create garden'}
                </Button>
              </Stack>
            </Stack>
          </form>
        </Card>
      </Container>
    </Box>
  )
}
