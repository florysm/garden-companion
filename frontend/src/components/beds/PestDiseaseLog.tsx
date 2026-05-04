import { useState } from 'react'
import {
  Box,
  Button,
  Card,
  Chip,
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
import CheckCircleOutlineOutlinedIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import BugReportOutlinedIcon from '@mui/icons-material/BugReportOutlined'
import LocalHospitalOutlinedIcon from '@mui/icons-material/LocalHospitalOutlined'
import SpaOutlinedIcon from '@mui/icons-material/SpaOutlined'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getPestDiseaseLogs,
  logPestDisease,
  resolvePestDiseaseLog,
  type PestDiseaseLog as PestDiseaseLogType,
  type PestDiseaseType,
  type Severity,
} from '../../api/gardens'

const PEST_DISEASE_TYPES: { value: PestDiseaseType; label: string }[] = [
  { value: 'Pest', label: 'Pest' },
  { value: 'Disease', label: 'Disease' },
  { value: 'NutrientDeficiency', label: 'Nutrient deficiency' },
]

const SEVERITIES: { value: Severity; label: string }[] = [
  { value: 'Low', label: 'Low' },
  { value: 'Medium', label: 'Medium' },
  { value: 'High', label: 'High' },
]

const SEVERITY_COLOR: Record<Severity, 'success' | 'warning' | 'error'> = {
  Low: 'success',
  Medium: 'warning',
  High: 'error',
}

const TYPE_ICON: Record<PestDiseaseType, React.ReactNode> = {
  Pest: <BugReportOutlinedIcon sx={{ fontSize: 16 }} />,
  Disease: <LocalHospitalOutlinedIcon sx={{ fontSize: 16 }} />,
  NutrientDeficiency: <SpaOutlinedIcon sx={{ fontSize: 16 }} />,
}

function today() {
  return new Date().toISOString().split('T')[0]
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function typeLabel(type: PestDiseaseType) {
  return PEST_DISEASE_TYPES.find(t => t.value === type)?.label ?? type
}

function LogRow({
  log,
  gardenId,
  bedId,
}: {
  log: PestDiseaseLogType
  gardenId: string
  bedId: string
}) {
  const queryClient = useQueryClient()
  const isResolved = log.resolvedAt !== null

  const resolveMutation = useMutation({
    mutationFn: () => resolvePestDiseaseLog(gardenId, bedId, log.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pestDisease', gardenId, bedId] })
    },
  })

  return (
    <Stack sx={{ gap: 0.5 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', gap: 2 }}>
        <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
          <Box sx={{ color: isResolved ? 'text.disabled' : 'secondary.main', display: 'flex', mt: 0.25 }}>
            {TYPE_ICON[log.type]}
          </Box>
          <Typography variant="body2" sx={{ fontWeight: 500, color: isResolved ? 'text.secondary' : 'text.primary' }}>
            {log.name}
          </Typography>
          <Chip
            label={typeLabel(log.type)}
            size="small"
            variant="outlined"
            sx={{ height: 18, fontSize: 11 }}
          />
          {!isResolved && (
            <Chip
              label={log.severity}
              size="small"
              color={SEVERITY_COLOR[log.severity]}
              sx={{ height: 18, fontSize: 11 }}
            />
          )}
          {isResolved && (
            <Chip
              label="Resolved"
              size="small"
              icon={<CheckCircleOutlineOutlinedIcon sx={{ fontSize: '14px !important' }} />}
              sx={{ height: 18, fontSize: 11, color: 'text.secondary', bgcolor: 'background.default' }}
            />
          )}
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0, mt: 0.25 }}>
          {formatDate(log.observedAt)}
        </Typography>
      </Stack>

      {log.treatmentApplied && (
        <Typography variant="body2" color="text.secondary" sx={{ pl: 3 }}>
          Treatment: {log.treatmentApplied}
        </Typography>
      )}
      {log.notes && (
        <Typography variant="body2" color="text.secondary" sx={{ pl: 3 }}>
          {log.notes}
        </Typography>
      )}

      {!isResolved && (
        <Box sx={{ pl: 3, mt: 0.5 }}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<CheckCircleOutlineOutlinedIcon />}
            disabled={resolveMutation.isPending}
            onClick={() => resolveMutation.mutate()}
            sx={{ color: 'primary.main', borderColor: 'primary.main', fontSize: 12, py: 0.25 }}
          >
            Mark resolved
          </Button>
          {resolveMutation.isError && (
            <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.5 }}>
              Could not resolve. Please try again.
            </Typography>
          )}
        </Box>
      )}
    </Stack>
  )
}

interface Props {
  gardenId: string
  bedId: string
}

export function PestDiseaseLog({ gardenId, bedId }: Props) {
  const queryClient = useQueryClient()
  const [adding, setAdding] = useState(false)
  const [type, setType] = useState<PestDiseaseType>('Pest')
  const [name, setName] = useState('')
  const [severity, setSeverity] = useState<Severity>('Medium')
  const [observedAt, setObservedAt] = useState(today())
  const [treatment, setTreatment] = useState('')
  const [notes, setNotes] = useState('')

  const { data: logs = [] } = useQuery({
    queryKey: ['pestDisease', gardenId, bedId],
    queryFn: () => getPestDiseaseLogs(gardenId, bedId),
  })

  const mutation = useMutation({
    mutationFn: () =>
      logPestDisease(gardenId, bedId, {
        type,
        name: name.trim(),
        severity,
        observedAt: observedAt ? new Date(observedAt + 'T00:00:00').toISOString() : undefined,
        treatmentApplied: treatment.trim() || undefined,
        notes: notes.trim() || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pestDisease', gardenId, bedId] })
      setAdding(false)
      resetForm()
    },
  })

  function resetForm() {
    setType('Pest')
    setName('')
    setSeverity('Medium')
    setObservedAt(today())
    setTreatment('')
    setNotes('')
  }

  const canSubmit = name.trim().length > 0 && !mutation.isPending

  const active = logs.filter(l => l.resolvedAt === null)
  const resolved = logs.filter(l => l.resolvedAt !== null)

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: logs.length > 0 ? 2 : 0 }}>
        <Stack sx={{ gap: 0.25 }}>
          <Typography variant="h6" sx={{ fontFamily: '"Spectral", serif' }}>
            Pests & Diseases
          </Typography>
          {active.length > 0 && (
            <Typography variant="caption" color="secondary.main">
              {active.length} active
            </Typography>
          )}
        </Stack>
        {!adding && (
          <Button size="small" startIcon={<AddOutlinedIcon />} onClick={() => setAdding(true)} sx={{ color: 'secondary.main' }}>
            Log issue
          </Button>
        )}
      </Stack>

      {active.length > 0 && (
        <Stack sx={{ gap: 0, mb: resolved.length > 0 ? 2 : 0 }}>
          {active.map((log, i) => (
            <Box key={log.id}>
              {i > 0 && <Divider sx={{ my: 1.5 }} />}
              <LogRow log={log} gardenId={gardenId} bedId={bedId} />
            </Box>
          ))}
        </Stack>
      )}

      {resolved.length > 0 && (
        <>
          {active.length > 0 && <Divider sx={{ mb: 2 }} />}
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1.5, textTransform: 'uppercase', letterSpacing: 0.5 }}>
            Resolved
          </Typography>
          <Stack sx={{ gap: 0 }}>
            {resolved.map((log, i) => (
              <Box key={log.id}>
                {i > 0 && <Divider sx={{ my: 1.5 }} />}
                <LogRow log={log} gardenId={gardenId} bedId={bedId} />
              </Box>
            ))}
          </Stack>
        </>
      )}

      {logs.length === 0 && !adding && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No pest or disease issues logged.
        </Typography>
      )}

      {adding && (
        <Box sx={{ mt: logs.length > 0 ? 2.5 : 1.5 }}>
          {logs.length > 0 && <Divider sx={{ mb: 2.5 }} />}
          <Stack sx={{ gap: 2 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <FormControl size="small" sx={{ minWidth: 160 }}>
                <InputLabel>Type</InputLabel>
                <Select label="Type" value={type} onChange={e => setType(e.target.value as PestDiseaseType)}>
                  {PEST_DISEASE_TYPES.map(t => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
                </Select>
              </FormControl>
              <TextField
                label="Name"
                size="small"
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder="e.g. Aphids, Powdery mildew"
                slotProps={{ htmlInput: { maxLength: 200 } }}
                sx={{ flex: 1 }}
              />
            </Stack>

            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <FormControl size="small" sx={{ minWidth: 130 }}>
                <InputLabel>Severity</InputLabel>
                <Select label="Severity" value={severity} onChange={e => setSeverity(e.target.value as Severity)}>
                  {SEVERITIES.map(s => <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>)}
                </Select>
              </FormControl>
              <TextField
                label="Observed on"
                type="date"
                size="small"
                value={observedAt}
                onChange={e => setObservedAt(e.target.value)}
                sx={{ flex: 1 }}
                slotProps={{ inputLabel: { shrink: true } }}
              />
            </Stack>

            <TextField
              label="Treatment applied"
              size="small"
              value={treatment}
              onChange={e => setTreatment(e.target.value)}
              placeholder="Optional — what did you use?"
              slotProps={{ htmlInput: { maxLength: 500 } }}
            />

            <TextField
              label="Notes"
              size="small"
              multiline
              minRows={2}
              maxRows={4}
              value={notes}
              onChange={e => setNotes(e.target.value)}
              slotProps={{ htmlInput: { maxLength: 1000 } }}
            />

            {mutation.isError && (
              <Typography variant="caption" color="error">
                Could not log issue. Please try again.
              </Typography>
            )}

            <Stack direction="row" sx={{ gap: 1, justifyContent: 'flex-end' }}>
              <Button size="small" onClick={() => { setAdding(false); resetForm() }} sx={{ color: 'text.secondary' }}>
                Cancel
              </Button>
              <Button size="small" variant="contained" color="secondary" disabled={!canSubmit} onClick={() => mutation.mutate()}>
                Log issue
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}
    </Card>
  )
}
