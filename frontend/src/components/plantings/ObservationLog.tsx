import { useState } from 'react'
import {
  Box,
  Button,
  Card,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import BugReportOutlinedIcon from '@mui/icons-material/BugReportOutlined'
import LocalHospitalOutlinedIcon from '@mui/icons-material/LocalHospitalOutlined'
import TrendingUpOutlinedIcon from '@mui/icons-material/TrendingUpOutlined'
import ScienceOutlinedIcon from '@mui/icons-material/ScienceOutlined'
import OpacityOutlinedIcon from '@mui/icons-material/OpacityOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addObservation,
  getObservations,
  type ObservationType,
} from '../../api/gardens'

const OBSERVATION_TYPES: { value: ObservationType; label: string }[] = [
  { value: 'General', label: 'General' },
  { value: 'Growth', label: 'Growth' },
  { value: 'Watered', label: 'Watered' },
  { value: 'Fertilized', label: 'Fertilized' },
  { value: 'Pest', label: 'Pest' },
  { value: 'Disease', label: 'Disease' },
]

const TYPE_ICON: Record<ObservationType, React.ReactNode> = {
  General: <VisibilityOutlinedIcon sx={{ fontSize: 16 }} />,
  Pest: <BugReportOutlinedIcon sx={{ fontSize: 16 }} />,
  Disease: <LocalHospitalOutlinedIcon sx={{ fontSize: 16 }} />,
  Growth: <TrendingUpOutlinedIcon sx={{ fontSize: 16 }} />,
  Fertilized: <ScienceOutlinedIcon sx={{ fontSize: 16 }} />,
  Watered: <OpacityOutlinedIcon sx={{ fontSize: 16 }} />,
}

const TYPE_COLOR: Record<ObservationType, string> = {
  General: 'text.secondary',
  Pest: 'secondary.main',
  Disease: 'error.main',
  Growth: 'primary.main',
  Fertilized: '#7B6FA0',
  Watered: '#4A90B8',
}

function formatObservedAt(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function today() {
  return new Date().toISOString().split('T')[0]
}

interface Props {
  plantingId: string
}

export function ObservationLog({ plantingId }: Props) {
  const queryClient = useQueryClient()
  const [adding, setAdding] = useState(false)
  const [type, setType] = useState<ObservationType>('General')
  const [note, setNote] = useState('')
  const [observedAt, setObservedAt] = useState(today())

  const { data: observations = [] } = useQuery({
    queryKey: ['observations', plantingId],
    queryFn: () => getObservations(plantingId),
  })

  const mutation = useMutation({
    mutationFn: () =>
      addObservation(plantingId, {
        observationType: type,
        note,
        observedAt: observedAt ? new Date(observedAt).toISOString() : undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['observations', plantingId] })
      queryClient.invalidateQueries({ queryKey: ['planting', plantingId] })
      setAdding(false)
      setType('General')
      setNote('')
      setObservedAt(today())
    },
  })

  const canSubmit = note.trim().length > 0 && !mutation.isPending

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: observations.length > 0 ? 2 : 0 }}>
        <Typography variant="h6" sx={{ fontFamily: '"Spectral", serif' }}>
          Observations
        </Typography>
        {!adding && (
          <Button
            size="small"
            startIcon={<AddOutlinedIcon />}
            onClick={() => setAdding(true)}
            sx={{ color: 'primary.main' }}
          >
            Add
          </Button>
        )}
      </Stack>

      {observations.length > 0 && (
        <Stack sx={{ gap: 0 }}>
          {observations.map((obs, i) => (
            <Box key={obs.id}>
              {i > 0 && <Divider sx={{ my: 1.5 }} />}
              <Stack direction="row" sx={{ gap: 1.5, alignItems: 'flex-start' }}>
                <Box sx={{ color: TYPE_COLOR[obs.observationType], mt: 0.25, flexShrink: 0 }}>
                  {TYPE_ICON[obs.observationType]}
                </Box>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 1, mb: 0.25 }}>
                    <Typography variant="caption" sx={{ color: TYPE_COLOR[obs.observationType], fontWeight: 500, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                      {obs.observationType}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0 }}>
                      {formatObservedAt(obs.observedAt)}
                    </Typography>
                  </Stack>
                  <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
                    {obs.note}
                  </Typography>
                </Box>
              </Stack>
            </Box>
          ))}
        </Stack>
      )}

      {observations.length === 0 && !adding && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No observations yet.
        </Typography>
      )}

      {adding && (
        <Box sx={{ mt: observations.length > 0 ? 2.5 : 1.5 }}>
          {observations.length > 0 && <Divider sx={{ mb: 2.5 }} />}
          <Stack sx={{ gap: 2 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <FormControl size="small" sx={{ minWidth: 160 }}>
                <InputLabel>Type</InputLabel>
                <Select
                  label="Type"
                  value={type}
                  onChange={e => setType(e.target.value as ObservationType)}
                >
                  {OBSERVATION_TYPES.map(t => (
                    <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField
                label="Date"
                type="date"
                size="small"
                value={observedAt}
                onChange={e => setObservedAt(e.target.value)}
                sx={{ flex: 1 }}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </Stack>
            <TextField
              label="Note"
              multiline
              minRows={2}
              maxRows={6}
              size="small"
              value={note}
              onChange={e => setNote(e.target.value)}
              placeholder="What did you observe?"
              slotProps={{ htmlInput: { maxLength: 2000 } }}
            />
            {mutation.isError && (
              <Typography variant="caption" color="error">
                Could not save observation. Please try again.
              </Typography>
            )}
            <Stack direction="row" sx={{ gap: 1, justifyContent: 'flex-end' }}>
              <Button
                size="small"
                onClick={() => { setAdding(false); setNote(''); setType('General'); setObservedAt(today()) }}
                sx={{ color: 'text.secondary' }}
              >
                Cancel
              </Button>
              <Button
                size="small"
                variant="contained"
                disabled={!canSubmit}
                onClick={() => mutation.mutate()}
              >
                Save
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}
    </Card>
  )
}
