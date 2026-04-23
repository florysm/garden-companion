import { useState, type FormEvent } from 'react'
import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createGardenTask } from '../api/tasks'
import { getGarden } from '../api/gardens'
import { WeatherStrip } from '../components/layout/WeatherStrip'

const TASK_TYPES = [
  { value: 'Water',     label: 'Water' },
  { value: 'Fertilize', label: 'Fertilize' },
  { value: 'Harvest',   label: 'Harvest' },
  { value: 'Prune',     label: 'Prune' },
  { value: 'Inspect',   label: 'Inspect' },
  { value: 'Amend',     label: 'Amend' },
  { value: 'Plant',     label: 'Plant' },
  { value: 'General',   label: 'General' },
]

export function CreateGardenTaskPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [title, setTitle] = useState('')
  const [taskType, setTaskType] = useState('General')
  const [dueDate, setDueDate] = useState('')
  const [description, setDescription] = useState('')
  const [gardenBedId, setGardenBedId] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  const { data: garden } = useQuery({
    queryKey: ['garden', id],
    queryFn: () => getGarden(id!),
    enabled: !!id,
  })

  const mutation = useMutation({
    mutationFn: (body: Parameters<typeof createGardenTask>[1]) => createGardenTask(id!, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', id] })
      navigate(`/gardens/${id}`)
    },
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const errs: Record<string, string> = {}
    if (!title.trim()) errs.title = 'Title is required.'
    if (Object.keys(errs).length > 0) {
      setErrors(errs)
      return
    }
    setErrors({})
    mutation.mutate({
      title: title.trim(),
      taskType,
      dueDate: dueDate || null,
      description: description.trim() || null,
      gardenBedId: gardenBedId || null,
      plantingId: null,
      assignedToUserId: null,
    })
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
          New Task
        </Typography>

        <Card sx={{ p: 4 }}>
          <form onSubmit={handleSubmit} noValidate>
            <Stack sx={{ gap: 3 }}>
              <TextField
                label="Title"
                value={title}
                onChange={e => setTitle(e.target.value)}
                autoFocus
                required
                error={!!errors.title}
                helperText={errors.title}
              />

              <Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                  Task type
                </Typography>
                <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
                  {TASK_TYPES.map(opt => {
                    const selected = taskType === opt.value
                    return (
                      <Chip
                        key={opt.value}
                        label={opt.label}
                        onClick={() => setTaskType(opt.value)}
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

              <TextField
                label="Due date"
                type="date"
                value={dueDate}
                onChange={e => setDueDate(e.target.value)}
                helperText="Optional"
                slotProps={{ inputLabel: { shrink: true } }}
              />

              {garden && garden.beds.length > 0 && (
                <FormControl fullWidth>
                  <InputLabel id="bed-label" shrink>
                    Bed
                  </InputLabel>
                  <Select
                    labelId="bed-label"
                    label="Bed"
                    value={gardenBedId}
                    onChange={e => setGardenBedId(e.target.value)}
                    displayEmpty
                    notched
                  >
                    <MenuItem value="">
                      <Typography variant="body2" color="text.secondary">
                        — any bed —
                      </Typography>
                    </MenuItem>
                    {garden.beds.map(bed => (
                      <MenuItem key={bed.id} value={bed.id}>
                        {bed.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}

              <TextField
                label="Description"
                value={description}
                onChange={e => setDescription(e.target.value)}
                multiline
                rows={3}
                helperText="Optional"
              />

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
                  {mutation.isPending ? 'Adding…' : 'Add task'}
                </Button>
              </Stack>
            </Stack>
          </form>
        </Card>
      </Container>
    </Box>
  )
}
