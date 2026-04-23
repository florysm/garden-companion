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
import ScienceOutlinedIcon from '@mui/icons-material/ScienceOutlined'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getSoilTests, createSoilTest, type SoilTestSource, type SoilTest } from '../../api/gardens'

const SOURCES: { value: SoilTestSource; label: string }[] = [
  { value: 'HomeKit', label: 'Home kit' },
  { value: 'LabTest', label: 'Lab test' },
  { value: 'Manual', label: 'Manual entry' },
]

function today() {
  return new Date().toISOString().split('T')[0]
}

function formatDate(dateStr: string) {
  return new Date(dateStr + 'T00:00:00').toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function phLabel(ph: number | null) {
  if (ph === null) return null
  if (ph < 6) return 'acidic'
  if (ph > 7.5) return 'alkaline'
  return 'neutral'
}

function SoilTestRow({ test }: { test: SoilTest }) {
  const nutrients = [
    test.nitrogenPpm !== null ? `N ${test.nitrogenPpm} ppm` : null,
    test.phosphorusPpm !== null ? `P ${test.phosphorusPpm} ppm` : null,
    test.potassiumPpm !== null ? `K ${test.potassiumPpm} ppm` : null,
    test.organicMatterPercent !== null ? `OM ${test.organicMatterPercent}%` : null,
  ].filter(Boolean)

  return (
    <Stack sx={{ gap: 0.5 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
        <Stack direction="row" sx={{ gap: 1, alignItems: 'center' }}>
          {test.phLevel !== null && (
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              pH {test.phLevel}
              {phLabel(test.phLevel) && (
                <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 0.5 }}>
                  ({phLabel(test.phLevel)})
                </Typography>
              )}
            </Typography>
          )}
          <Typography variant="caption" color="text.secondary">
            {test.testSource}
          </Typography>
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0 }}>
          {formatDate(test.testedAt)}
        </Typography>
      </Stack>
      {nutrients.length > 0 && (
        <Typography variant="caption" color="text.secondary">
          {nutrients.join(' · ')}
        </Typography>
      )}
      {test.notes && (
        <Typography variant="body2" color="text.secondary">
          {test.notes}
        </Typography>
      )}
    </Stack>
  )
}

interface Props {
  gardenId: string
  bedId: string
}

export function SoilTestLog({ gardenId, bedId }: Props) {
  const queryClient = useQueryClient()
  const [adding, setAdding] = useState(false)
  const [testedAt, setTestedAt] = useState(today())
  const [source, setSource] = useState<SoilTestSource>('HomeKit')
  const [ph, setPh] = useState('')
  const [nitrogen, setNitrogen] = useState('')
  const [phosphorus, setPhosphorus] = useState('')
  const [potassium, setPotassium] = useState('')
  const [organicMatter, setOrganicMatter] = useState('')
  const [notes, setNotes] = useState('')

  const { data: tests = [] } = useQuery({
    queryKey: ['soilTests', gardenId, bedId],
    queryFn: () => getSoilTests(gardenId, bedId),
  })

  const mutation = useMutation({
    mutationFn: () =>
      createSoilTest(gardenId, bedId, {
        testedAt,
        testSource: source,
        phLevel: ph ? parseFloat(ph) : undefined,
        nitrogenPpm: nitrogen ? parseFloat(nitrogen) : undefined,
        phosphorusPpm: phosphorus ? parseFloat(phosphorus) : undefined,
        potassiumPpm: potassium ? parseFloat(potassium) : undefined,
        organicMatterPercent: organicMatter ? parseFloat(organicMatter) : undefined,
        notes: notes.trim() || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['soilTests', gardenId, bedId] })
      setAdding(false)
      resetForm()
    },
  })

  function resetForm() {
    setTestedAt(today())
    setSource('HomeKit')
    setPh('')
    setNitrogen('')
    setPhosphorus('')
    setPotassium('')
    setOrganicMatter('')
    setNotes('')
  }

  const canSubmit = testedAt.length > 0 && !mutation.isPending

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: tests.length > 0 ? 2 : 0 }}>
        <Stack direction="row" sx={{ gap: 1, alignItems: 'center' }}>
          <ScienceOutlinedIcon sx={{ fontSize: 20, color: 'primary.main' }} />
          <Typography variant="h6" sx={{ fontFamily: '"Spectral", serif' }}>
            Soil Tests
          </Typography>
        </Stack>
        {!adding && (
          <Button size="small" startIcon={<AddOutlinedIcon />} onClick={() => setAdding(true)} sx={{ color: 'primary.main' }}>
            Add test
          </Button>
        )}
      </Stack>

      {tests.length > 0 && (
        <Stack sx={{ gap: 0 }}>
          {tests.map((t, i) => (
            <Box key={t.id}>
              {i > 0 && <Divider sx={{ my: 1.5 }} />}
              <SoilTestRow test={t} />
            </Box>
          ))}
        </Stack>
      )}

      {tests.length === 0 && !adding && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No soil tests recorded yet.
        </Typography>
      )}

      {adding && (
        <Box sx={{ mt: tests.length > 0 ? 2.5 : 1.5 }}>
          {tests.length > 0 && <Divider sx={{ mb: 2.5 }} />}
          <Stack sx={{ gap: 2 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <TextField
                label="Test date"
                type="date"
                size="small"
                value={testedAt}
                onChange={e => setTestedAt(e.target.value)}
                sx={{ flex: 1 }}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <FormControl size="small" sx={{ minWidth: 140 }}>
                <InputLabel>Source</InputLabel>
                <Select label="Source" value={source} onChange={e => setSource(e.target.value as SoilTestSource)}>
                  {SOURCES.map(s => <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>)}
                </Select>
              </FormControl>
            </Stack>

            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <TextField
                label="pH (0–14)"
                type="number"
                size="small"
                value={ph}
                onChange={e => setPh(e.target.value)}
                inputProps={{ min: 0, max: 14, step: 0.1 }}
                sx={{ flex: 1 }}
              />
              <TextField
                label="Organic matter %"
                type="number"
                size="small"
                value={organicMatter}
                onChange={e => setOrganicMatter(e.target.value)}
                inputProps={{ min: 0, max: 100, step: 0.1 }}
                sx={{ flex: 1 }}
              />
            </Stack>

            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <TextField label="Nitrogen (ppm)" type="number" size="small" value={nitrogen} onChange={e => setNitrogen(e.target.value)} inputProps={{ min: 0 }} sx={{ flex: 1 }} />
              <TextField label="Phosphorus (ppm)" type="number" size="small" value={phosphorus} onChange={e => setPhosphorus(e.target.value)} inputProps={{ min: 0 }} sx={{ flex: 1 }} />
              <TextField label="Potassium (ppm)" type="number" size="small" value={potassium} onChange={e => setPotassium(e.target.value)} inputProps={{ min: 0 }} sx={{ flex: 1 }} />
            </Stack>

            <TextField
              label="Notes"
              size="small"
              multiline
              minRows={2}
              maxRows={4}
              value={notes}
              onChange={e => setNotes(e.target.value)}
              inputProps={{ maxLength: 500 }}
            />

            {mutation.isError && (
              <Typography variant="caption" color="error">
                Could not save soil test. Please try again.
              </Typography>
            )}

            <Stack direction="row" sx={{ gap: 1, justifyContent: 'flex-end' }}>
              <Button size="small" onClick={() => { setAdding(false); resetForm() }} sx={{ color: 'text.secondary' }}>
                Cancel
              </Button>
              <Button size="small" variant="contained" disabled={!canSubmit} onClick={() => mutation.mutate()}>
                Save
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}
    </Card>
  )
}
