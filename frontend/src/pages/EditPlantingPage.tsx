import { useState, useEffect, type FormEvent } from 'react'
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
import { getPlanting, updatePlanting } from '../api/gardens'
import { AppHeader } from '../components/layout/AppHeader'

const PLANTING_TYPES = [
  { value: 'Annual', label: 'Annual' },
  { value: 'Perennial', label: 'Perennial' },
  { value: 'Biennial', label: 'Biennial' },
]

const PLANTING_SOURCES = [
  { value: 'DirectSeed', label: 'Direct Seed' },
  { value: 'IndoorSeedStart', label: 'Indoor Start' },
  { value: 'PurchasedTransplant', label: 'Purchased Transplant' },
]

const SEASON_TYPES = [
  { value: 'Spring', label: 'Spring' },
  { value: 'Summer', label: 'Summer' },
  { value: 'Fall', label: 'Fall' },
  { value: 'Winter', label: 'Winter' },
  { value: 'YearRound', label: 'Year Round' },
]

function ChipGroup({
  label,
  options,
  value,
  onChange,
}: {
  label: string
  options: { value: string; label: string }[]
  value: string
  onChange: (v: string) => void
}) {
  return (
    <Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
        {label}
      </Typography>
      <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
        {options.map(opt => {
          const selected = value === opt.value
          return (
            <Chip
              key={opt.value}
              label={opt.label}
              onClick={() => onChange(opt.value)}
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
  )
}

export function EditPlantingPage() {
  const { id: gardenId, plantingId } = useParams<{ id: string; plantingId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: planting, isLoading } = useQuery({
    queryKey: ['planting', plantingId],
    queryFn: () => getPlanting(plantingId!),
    enabled: !!plantingId,
  })

  const [expectedHarvestDate, setExpectedHarvestDate] = useState('')
  const [plantingType, setPlantingType] = useState('Annual')
  const [source, setSource] = useState('DirectSeed')
  const [quantity, setQuantity] = useState('1')
  const [seasonYear, setSeasonYear] = useState('')
  const [seasonType, setSeasonType] = useState('Spring')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [initialized, setInitialized] = useState(false)

  useEffect(() => {
    if (planting && !initialized) {
      setExpectedHarvestDate(planting.expectedHarvestDate ?? '')
      setPlantingType(planting.plantingType)
      setSource(planting.source)
      setQuantity(planting.quantity.toString())
      setSeasonYear(planting.seasonYear.toString())
      setSeasonType(planting.seasonType)
      setInitialized(true)
    }
  }, [planting, initialized])

  const mutation = useMutation({
    mutationFn: () =>
      updatePlanting(plantingId!, {
        expectedHarvestDate: expectedHarvestDate || null,
        plantingType,
        source: source as import('../api/gardens').PlantingSource,
        quantity: parseInt(quantity, 10),
        seasonYear: parseInt(seasonYear, 10) || new Date().getFullYear(),
        seasonType,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['planting', plantingId], updated)
      queryClient.invalidateQueries({ queryKey: ['plantings', gardenId, planting?.gardenBedId] })
      navigate(`/gardens/${gardenId}/plantings/${plantingId}`)
    },
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const errs: Record<string, string> = {}
    const qty = parseInt(quantity, 10)
    if (isNaN(qty) || qty < 1) errs.quantity = 'Quantity must be at least 1.'
    if (Object.keys(errs).length > 0) {
      setErrors(errs)
      return
    }
    setErrors({})
    mutation.mutate()
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppHeader />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/gardens/${gardenId}/plantings/${plantingId}`)}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back to planting
        </Button>

        <Typography variant="h4" sx={{ mb: planting ? 1 : 4 }}>
          Edit Planting
        </Typography>
        {planting && (
          <Typography variant="body1" color="text.secondary" sx={{ fontStyle: 'italic', mb: 4 }}>
            {planting.plantCommonName}
          </Typography>
        )}

        {isLoading && (
          <Stack sx={{ gap: 3 }}>
            <Skeleton variant="rounded" height={56} />
            <Skeleton variant="rounded" height={60} />
            <Skeleton variant="rounded" height={56} />
          </Stack>
        )}

        {!isLoading && planting && (
          <Card sx={{ p: 4 }}>
            <form onSubmit={handleSubmit} noValidate>
              <Stack sx={{ gap: 3 }}>
                {/* Planted date — read-only context */}
                <Stack
                  direction="row"
                  sx={{ justifyContent: 'space-between', p: 1.5, bgcolor: 'background.default', borderRadius: 2 }}
                >
                  <Typography variant="body2" color="text.secondary">Planted</Typography>
                  <Typography variant="body2">
                    {new Date(planting.plantedDate + 'T00:00:00').toLocaleDateString(undefined, {
                      month: 'long', day: 'numeric', year: 'numeric',
                    })}
                  </Typography>
                </Stack>

                <TextField
                  label="Expected harvest date"
                  type="date"
                  value={expectedHarvestDate}
                  onChange={e => setExpectedHarvestDate(e.target.value)}
                  helperText="Optional"
                  slotProps={{ inputLabel: { shrink: true } }}
                />

                <TextField
                  label="Quantity"
                  type="number"
                  value={quantity}
                  onChange={e => setQuantity(e.target.value)}
                  required
                  error={!!errors.quantity}
                  helperText={errors.quantity || 'Number of plants'}
                  slotProps={{ htmlInput: { min: 1, max: 10000, step: 1 } }}
                />

                <ChipGroup
                  label="Planting type"
                  options={PLANTING_TYPES}
                  value={plantingType}
                  onChange={setPlantingType}
                />

                <ChipGroup
                  label="How did you get this plant?"
                  options={PLANTING_SOURCES}
                  value={source}
                  onChange={setSource}
                />

                <TextField
                  label="Season year"
                  type="number"
                  value={seasonYear}
                  onChange={e => setSeasonYear(e.target.value)}
                  slotProps={{ htmlInput: { min: 2000, max: 2100, step: 1 } }}
                />

                <ChipGroup
                  label="Season"
                  options={SEASON_TYPES}
                  value={seasonType}
                  onChange={setSeasonType}
                />

                {mutation.isError && (
                  <Typography variant="body2" color="error">
                    Something went wrong. Please try again.
                  </Typography>
                )}

                <Stack direction="row" sx={{ gap: 2, justifyContent: 'flex-end', mt: 1 }}>
                  <Button
                    onClick={() => navigate(`/gardens/${gardenId}/plantings/${plantingId}`)}
                    color="inherit"
                  >
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
