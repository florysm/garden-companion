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
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getAmendmentLogs,
  logAmendment,
  type AmendmentLog as AmendmentLogType,
  type AmendmentType,
  type QuantityUnit,
} from '../../api/gardens'

const AMENDMENT_TYPES: { value: AmendmentType; label: string }[] = [
  { value: 'Fertilizer', label: 'Fertilizer' },
  { value: 'Compost', label: 'Compost' },
  { value: 'Mulch', label: 'Mulch' },
  { value: 'PhAdjuster', label: 'pH Adjuster' },
  { value: 'Pesticide', label: 'Pesticide' },
  { value: 'HerbControl', label: 'Herb control' },
  { value: 'Other', label: 'Other' },
]

const UNITS: { value: QuantityUnit; label: string }[] = [
  { value: 'Count', label: 'count' },
  { value: 'Pounds', label: 'lbs' },
  { value: 'Ounces', label: 'oz' },
  { value: 'Grams', label: 'g' },
  { value: 'Kilograms', label: 'kg' },
  { value: 'Gallons', label: 'gal' },
  { value: 'Liters', label: 'L' },
  { value: 'Milliliters', label: 'mL' },
]

const TYPE_COLOR: Record<AmendmentType, string> = {
  Fertilizer: 'primary',
  Compost: 'primary',
  Mulch: 'default',
  PhAdjuster: 'default',
  Pesticide: 'error',
  HerbControl: 'warning',
  Other: 'default',
} as const

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

function unitLabel(unit: QuantityUnit) {
  return UNITS.find(u => u.value === unit)?.label ?? unit
}

function typeLabel(type: AmendmentType) {
  return AMENDMENT_TYPES.find(t => t.value === type)?.label ?? type
}

function AmendmentRow({ log }: { log: AmendmentLogType }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', gap: 2 }}>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Stack direction="row" sx={{ gap: 1, alignItems: 'center', mb: 0.25, flexWrap: 'wrap' }}>
          <Typography variant="body2" sx={{ fontWeight: 500 }}>
            {log.productName}
          </Typography>
          <Chip
            label={typeLabel(log.amendmentType)}
            size="small"
            color={TYPE_COLOR[log.amendmentType] as 'primary' | 'error' | 'warning' | 'default'}
            variant="outlined"
            sx={{ height: 18, fontSize: 11 }}
          />
        </Stack>
        <Typography variant="caption" color="text.secondary">
          {log.quantity} {unitLabel(log.quantityUnit)}
        </Typography>
        {log.notes && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
            {log.notes}
          </Typography>
        )}
      </Box>
      <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0, mt: 0.25 }}>
        {formatDate(log.appliedAt)}
      </Typography>
    </Stack>
  )
}

interface Props {
  gardenId: string
  bedId: string
}

export function AmendmentLog({ gardenId, bedId }: Props) {
  const queryClient = useQueryClient()
  const [adding, setAdding] = useState(false)
  const [appliedAt, setAppliedAt] = useState(today())
  const [productName, setProductName] = useState('')
  const [amendmentType, setAmendmentType] = useState<AmendmentType>('Fertilizer')
  const [quantity, setQuantity] = useState('')
  const [unit, setUnit] = useState<QuantityUnit>('Pounds')
  const [notes, setNotes] = useState('')

  const { data: logs = [] } = useQuery({
    queryKey: ['amendments', gardenId, bedId],
    queryFn: () => getAmendmentLogs(gardenId, bedId),
  })

  const mutation = useMutation({
    mutationFn: () =>
      logAmendment(gardenId, bedId, {
        appliedAt,
        productName: productName.trim(),
        amendmentType,
        quantity: parseFloat(quantity),
        quantityUnit: unit,
        notes: notes.trim() || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['amendments', gardenId, bedId] })
      setAdding(false)
      resetForm()
    },
  })

  function resetForm() {
    setAppliedAt(today())
    setProductName('')
    setAmendmentType('Fertilizer')
    setQuantity('')
    setUnit('Pounds')
    setNotes('')
  }

  const parsedQty = parseFloat(quantity)
  const canSubmit = productName.trim().length > 0 && !isNaN(parsedQty) && parsedQty > 0 && appliedAt.length > 0 && !mutation.isPending

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: logs.length > 0 ? 2 : 0 }}>
        <Typography variant="h6" sx={{ fontFamily: '"Spectral", serif' }}>
          Amendments
        </Typography>
        {!adding && (
          <Button size="small" startIcon={<AddOutlinedIcon />} onClick={() => setAdding(true)} sx={{ color: 'primary.main' }}>
            Log amendment
          </Button>
        )}
      </Stack>

      {logs.length > 0 && (
        <Stack sx={{ gap: 0 }}>
          {logs.map((log, i) => (
            <Box key={log.id}>
              {i > 0 && <Divider sx={{ my: 1.5 }} />}
              <AmendmentRow log={log} />
            </Box>
          ))}
        </Stack>
      )}

      {logs.length === 0 && !adding && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No amendments logged yet.
        </Typography>
      )}

      {adding && (
        <Box sx={{ mt: logs.length > 0 ? 2.5 : 1.5 }}>
          {logs.length > 0 && <Divider sx={{ mb: 2.5 }} />}
          <Stack sx={{ gap: 2 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <TextField
                label="Product name"
                size="small"
                value={productName}
                onChange={e => setProductName(e.target.value)}
                slotProps={{ htmlInput: { maxLength: 200 } }}
                sx={{ flex: 2 }}
              />
              <FormControl size="small" sx={{ flex: 1, minWidth: 140 }}>
                <InputLabel>Type</InputLabel>
                <Select label="Type" value={amendmentType} onChange={e => setAmendmentType(e.target.value as AmendmentType)}>
                  {AMENDMENT_TYPES.map(t => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
                </Select>
              </FormControl>
            </Stack>

            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <TextField
                label="Date applied"
                type="date"
                size="small"
                value={appliedAt}
                onChange={e => setAppliedAt(e.target.value)}
                sx={{ flex: 1 }}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <TextField
                label="Quantity"
                type="number"
                size="small"
                value={quantity}
                onChange={e => setQuantity(e.target.value)}
                slotProps={{ htmlInput: { min: 0, step: 'any' } }}
                sx={{ flex: 1 }}
              />
              <FormControl size="small" sx={{ minWidth: 100 }}>
                <InputLabel>Unit</InputLabel>
                <Select label="Unit" value={unit} onChange={e => setUnit(e.target.value as QuantityUnit)}>
                  {UNITS.map(u => <MenuItem key={u.value} value={u.value}>{u.label}</MenuItem>)}
                </Select>
              </FormControl>
            </Stack>

            <TextField
              label="Notes"
              size="small"
              multiline
              minRows={2}
              maxRows={4}
              value={notes}
              onChange={e => setNotes(e.target.value)}
              slotProps={{ htmlInput: { maxLength: 500 } }}
            />

            {mutation.isError && (
              <Typography variant="caption" color="error">
                Could not log amendment. Please try again.
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
