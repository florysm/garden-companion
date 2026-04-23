import { useState, type FormEvent } from 'react'
import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  Skeleton,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getGarden, getGardenTypes, updateGarden } from '../api/gardens'
import { WeatherStrip } from '../components/layout/WeatherStrip'

export function EditGardenPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: garden, isLoading: gardenLoading } = useQuery({
    queryKey: ['garden', id],
    queryFn: () => getGarden(id!),
    enabled: !!id,
  })

  const { data: gardenTypes = [] } = useQuery({
    queryKey: ['garden-types'],
    queryFn: getGardenTypes,
  })

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedTypeIds, setSelectedTypeIds] = useState<number[]>([])
  const [nameError, setNameError] = useState('')
  const [initialized, setInitialized] = useState(false)

  if (garden && gardenTypes.length > 0 && !initialized) {
    setName(garden.name)
    setDescription(garden.description ?? '')
    const matchedIds = gardenTypes
      .filter(t => garden.types.includes(t.name))
      .map(t => t.id)
    setSelectedTypeIds(matchedIds)
    setInitialized(true)
  }

  const mutation = useMutation({
    mutationFn: () =>
      updateGarden(id!, {
        name: name.trim(),
        description: description.trim() || null,
        gardenTypeIds: selectedTypeIds,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['garden', id], updated)
      queryClient.invalidateQueries({ queryKey: ['gardens'] })
      navigate(`/gardens/${id}`)
    },
  })

  function toggleType(typeId: number) {
    setSelectedTypeIds(prev =>
      prev.includes(typeId) ? prev.filter(t => t !== typeId) : [...prev, typeId]
    )
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!name.trim()) {
      setNameError('Garden name is required.')
      return
    }
    setNameError('')
    mutation.mutate()
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <WeatherStrip />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/gardens/${id}`)}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back to garden
        </Button>

        <Typography variant="h4" sx={{ mb: 4 }}>
          Edit Garden
        </Typography>

        {gardenLoading && (
          <Stack sx={{ gap: 3 }}>
            <Skeleton variant="rounded" height={56} />
            <Skeleton variant="rounded" height={100} />
            <Skeleton variant="rounded" height={40} />
          </Stack>
        )}

        {!gardenLoading && garden && (
          <Card sx={{ p: 4 }}>
            <form onSubmit={handleSubmit} noValidate>
              <Stack sx={{ gap: 3 }}>
                <TextField
                  label="Garden name"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  required
                  error={!!nameError}
                  helperText={nameError}
                  autoFocus
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
                              '&:hover': { bgcolor: selected ? 'primary.dark' : '#E8E5DE' },
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
                  <Button onClick={() => navigate(`/gardens/${id}`)} color="inherit">
                    Cancel
                  </Button>
                  <Button
                    type="submit"
                    variant="contained"
                    color="secondary"
                    disabled={mutation.isPending}
                  >
                    {mutation.isPending ? 'Saving…' : 'Save changes'}
                  </Button>
                </Stack>
              </Stack>
            </form>
          </Card>
        )}
      </Container>
    </Box>
  )
}
