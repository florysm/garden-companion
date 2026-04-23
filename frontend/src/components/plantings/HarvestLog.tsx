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
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getHarvestLogs,
  logHarvest,
  type QuantityUnit,
} from '../../api/gardens'

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

function formatDate(dateStr: string) {
  return new Date(dateStr + 'T00:00:00').toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function today() {
  return new Date().toISOString().split('T')[0]
}

function unitLabel(unit: QuantityUnit): string {
  return UNITS.find(u => u.value === unit)?.label ?? unit
}

interface Props {
  plantingId: string
}

export function HarvestLog({ plantingId }: Props) {
  const queryClient = useQueryClient()
  const [adding, setAdding] = useState(false)
  const [harvestDate, setHarvestDate] = useState(today())
  const [quantity, setQuantity] = useState('')
  const [unit, setUnit] = useState<QuantityUnit>('Count')
  const [notes, setNotes] = useState('')

  const { data: harvests = [] } = useQuery({
    queryKey: ['harvests', plantingId],
    queryFn: () => getHarvestLogs(plantingId),
  })

  const mutation = useMutation({
    mutationFn: () =>
      logHarvest(plantingId, {
        harvestDate,
        quantity: parseFloat(quantity),
        quantityUnit: unit,
        notes: notes.trim() || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['harvests', plantingId] })
      queryClient.invalidateQueries({ queryKey: ['planting', plantingId] })
      setAdding(false)
      setHarvestDate(today())
      setQuantity('')
      setUnit('Count')
      setNotes('')
    },
  })

  const parsedQty = parseFloat(quantity)
  const canSubmit = !isNaN(parsedQty) && parsedQty > 0 && harvestDate.length > 0 && !mutation.isPending

  const totalByUnit = harvests.reduce<Partial<Record<QuantityUnit, number>>>((acc, h) => {
    acc[h.quantityUnit] = (acc[h.quantityUnit] ?? 0) + h.quantity
    return acc
  }, {})

  return (
    <Card sx={{ p: 3 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: harvests.length > 0 ? 2 : 0 }}>
        <Stack sx={{ gap: 0.25 }}>
          <Typography variant="h6" sx={{ fontFamily: '"Spectral", serif' }}>
            Harvest Log
          </Typography>
          {harvests.length > 0 && (
            <Typography variant="caption" color="text.secondary">
              Total:{' '}
              {Object.entries(totalByUnit)
                .map(([u, v]) => `${v} ${unitLabel(u as QuantityUnit)}`)
                .join(' · ')}
            </Typography>
          )}
        </Stack>
        {!adding && (
          <Button
            size="small"
            startIcon={<AddOutlinedIcon />}
            onClick={() => setAdding(true)}
            sx={{ color: 'primary.main' }}
          >
            Log harvest
          </Button>
        )}
      </Stack>

      {harvests.length > 0 && (
        <Stack sx={{ gap: 0 }}>
          {harvests.map((h, i) => (
            <Box key={h.id}>
              {i > 0 && <Divider sx={{ my: 1.5 }} />}
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', gap: 2 }}>
                <Box>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {h.quantity} {unitLabel(h.quantityUnit)}
                  </Typography>
                  {h.notes && (
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                      {h.notes}
                    </Typography>
                  )}
                  <Typography variant="caption" color="text.secondary">
                    by {h.harvestedByDisplayName}
                  </Typography>
                </Box>
                <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0, mt: 0.25 }}>
                  {formatDate(h.harvestDate)}
                </Typography>
              </Stack>
            </Box>
          ))}
        </Stack>
      )}

      {harvests.length === 0 && !adding && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No harvests logged yet.
        </Typography>
      )}

      {adding && (
        <Box sx={{ mt: harvests.length > 0 ? 2.5 : 1.5 }}>
          {harvests.length > 0 && <Divider sx={{ mb: 2.5 }} />}
          <Stack sx={{ gap: 2 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
              <TextField
                label="Date"
                type="date"
                size="small"
                value={harvestDate}
                onChange={e => setHarvestDate(e.target.value)}
                sx={{ flex: 1 }}
                slotProps={{ inputLabel: { shrink: true } }}
              />
              <TextField
                label="Quantity"
                type="number"
                size="small"
                value={quantity}
                onChange={e => setQuantity(e.target.value)}
                inputProps={{ min: 0, step: 'any' }}
                sx={{ flex: 1 }}
              />
              <FormControl size="small" sx={{ minWidth: 100 }}>
                <InputLabel>Unit</InputLabel>
                <Select
                  label="Unit"
                  value={unit}
                  onChange={e => setUnit(e.target.value as QuantityUnit)}
                >
                  {UNITS.map(u => (
                    <MenuItem key={u.value} value={u.value}>{u.label}</MenuItem>
                  ))}
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
              placeholder="Optional notes about this harvest"
              inputProps={{ maxLength: 500 }}
            />
            {mutation.isError && (
              <Typography variant="caption" color="error">
                Could not log harvest. Please try again.
              </Typography>
            )}
            <Stack direction="row" sx={{ gap: 1, justifyContent: 'flex-end' }}>
              <Button
                size="small"
                onClick={() => { setAdding(false); setQuantity(''); setNotes(''); setHarvestDate(today()); setUnit('Count') }}
                sx={{ color: 'text.secondary' }}
              >
                Cancel
              </Button>
              <Button
                size="small"
                variant="contained"
                color="secondary"
                disabled={!canSubmit}
                onClick={() => mutation.mutate()}
              >
                Log harvest
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}
    </Card>
  )
}
