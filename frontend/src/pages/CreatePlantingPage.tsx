import { useState, useEffect, type FormEvent } from 'react'
import {
  Box,
  Button,
  Card,
  Chip,
  CircularProgress,
  Container,
  Divider,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createPlanting } from '../api/gardens'
import { searchPlants, type PlantSummary } from '../api/plants'
import { WeatherStrip } from '../components/layout/WeatherStrip'

const PLANTING_TYPES = [
  { value: 'Annual', label: 'Annual' },
  { value: 'Perennial', label: 'Perennial' },
  { value: 'Biennial', label: 'Biennial' },
]

function todayIso() {
  return new Date().toISOString().split('T')[0]
}

function addDays(iso: string, days: number) {
  const d = new Date(iso + 'T00:00:00')
  d.setDate(d.getDate() + days)
  return d.toISOString().split('T')[0]
}

function useDebounce(value: string, delay: number) {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(t)
  }, [value, delay])
  return debounced
}

function PlantSearchResult({
  plant,
  onSelect,
}: {
  plant: PlantSummary
  onSelect: (p: PlantSummary) => void
}) {
  return (
    <Box
      sx={{
        px: 2,
        py: 1.5,
        cursor: 'pointer',
        borderRadius: 1,
        '&:hover': { bgcolor: '#E8E5DE' },
      }}
      onClick={() => onSelect(plant)}
    >
      <Typography variant="body1" sx={{ fontStyle: 'italic', fontFamily: '"Spectral", serif' }}>
        {plant.commonName}
      </Typography>
      {plant.scientificName && (
        <Typography variant="caption" color="text.secondary">
          {plant.scientificName}
          {plant.daysToMaturity ? ` · ${plant.daysToMaturity} days to maturity` : ''}
        </Typography>
      )}
    </Box>
  )
}

export function CreatePlantingPage() {
  const { id: gardenId, bedId } = useParams<{ id: string; bedId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [searchInput, setSearchInput] = useState('')
  const [selectedPlant, setSelectedPlant] = useState<PlantSummary | null>(null)
  const [plantedDate, setPlantedDate] = useState(todayIso)
  const [quantity, setQuantity] = useState('1')
  const [plantingType, setPlantingType] = useState('Annual')
  const [overrideHarvest, setOverrideHarvest] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  const debouncedSearch = useDebounce(searchInput, 300)

  const { data: searchResults = [], isFetching: searching } = useQuery({
    queryKey: ['plants-search', debouncedSearch],
    queryFn: () => searchPlants(debouncedSearch),
    enabled: debouncedSearch.trim().length >= 2 && !selectedPlant,
  })

  const autoHarvestDate =
    selectedPlant?.daysToMaturity && plantedDate
      ? addDays(plantedDate, selectedPlant.daysToMaturity)
      : null

  const effectiveHarvestDate = overrideHarvest || autoHarvestDate || null

  const mutation = useMutation({
    mutationFn: (body: Parameters<typeof createPlanting>[1]) => createPlanting(gardenId!, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['plantings', gardenId, bedId] })
      queryClient.invalidateQueries({ queryKey: ['bed', gardenId, bedId] })
      queryClient.invalidateQueries({ queryKey: ['garden', gardenId] })
      navigate(`/gardens/${gardenId}/beds/${bedId}`)
    },
  })

  function handleSelectPlant(plant: PlantSummary) {
    setSelectedPlant(plant)
    setSearchInput('')
    setOverrideHarvest('')
    setErrors(prev => ({ ...prev, plant: '' }))
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const errs: Record<string, string> = {}
    if (!selectedPlant) errs.plant = 'Select a plant.'
    if (!plantedDate) errs.plantedDate = 'Planted date is required.'
    const qty = parseInt(quantity, 10)
    if (isNaN(qty) || qty < 1) errs.quantity = 'Quantity must be at least 1.'
    if (Object.keys(errs).length > 0) {
      setErrors(errs)
      return
    }
    setErrors({})
    mutation.mutate({
      gardenBedId: bedId!,
      plantId: selectedPlant!.id,
      plantedDate,
      expectedHarvestDate: effectiveHarvestDate,
      plantingType,
      quantity: qty,
      seasonYear: null,
      seasonType: null,
    })
  }

  const showResults = debouncedSearch.trim().length >= 2 && !selectedPlant

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <WeatherStrip />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/gardens/${gardenId}/beds/${bedId}`)}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back to bed
        </Button>

        <Typography variant="h4" sx={{ mb: 4 }}>
          Add Planting
        </Typography>

        <Card sx={{ p: 4 }}>
          <form onSubmit={handleSubmit} noValidate>
            <Stack sx={{ gap: 3 }}>

              {/* Plant search / selection */}
              <Box>
                <Typography variant="body2" color={errors.plant ? 'error' : 'text.secondary'} sx={{ mb: 1.5 }}>
                  Plant
                </Typography>

                {selectedPlant ? (
                  <Stack direction="row" sx={{ alignItems: 'center', gap: 1.5, p: 1.5, bgcolor: 'background.default', borderRadius: 2 }}>
                    <CheckCircleOutlinedIcon sx={{ color: 'primary.main', fontSize: 20 }} />
                    <Box sx={{ flex: 1 }}>
                      <Typography variant="body1" sx={{ fontStyle: 'italic', fontFamily: '"Spectral", serif' }}>
                        {selectedPlant.commonName}
                      </Typography>
                      {selectedPlant.scientificName && (
                        <Typography variant="caption" color="text.secondary">
                          {selectedPlant.scientificName}
                        </Typography>
                      )}
                    </Box>
                    <Button
                      size="small"
                      onClick={() => { setSelectedPlant(null); setOverrideHarvest('') }}
                      sx={{ color: 'text.secondary', minWidth: 0 }}
                    >
                      Change
                    </Button>
                  </Stack>
                ) : (
                  <Box>
                    <TextField
                      placeholder="Search by name or family…"
                      value={searchInput}
                      onChange={e => setSearchInput(e.target.value)}
                      error={!!errors.plant}
                      helperText={errors.plant}
                      slotProps={{
                        input: {
                          startAdornment: (
                            <InputAdornment position="start">
                              {searching
                                ? <CircularProgress size={16} color="inherit" />
                                : <SearchOutlinedIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
                              }
                            </InputAdornment>
                          ),
                        },
                      }}
                    />
                    {showResults && (
                      <Card sx={{ mt: 0.5, p: 0.5, maxHeight: 260, overflowY: 'auto' }}>
                        {searchResults.length === 0 && !searching && (
                          <Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
                            No plants found.
                          </Typography>
                        )}
                        {searchResults.map((plant, i) => (
                          <Box key={plant.id}>
                            {i > 0 && <Divider />}
                            <PlantSearchResult plant={plant} onSelect={handleSelectPlant} />
                          </Box>
                        ))}
                      </Card>
                    )}
                  </Box>
                )}
              </Box>

              <TextField
                label="Planted date"
                type="date"
                value={plantedDate}
                onChange={e => { setPlantedDate(e.target.value); setOverrideHarvest('') }}
                required
                error={!!errors.plantedDate}
                helperText={errors.plantedDate}
                slotProps={{ inputLabel: { shrink: true } }}
              />

              {autoHarvestDate && !overrideHarvest && (
                <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', px: 0.5 }}>
                  <Typography variant="body2" color="text.secondary">
                    Expected harvest ~{new Date(autoHarvestDate + 'T00:00:00').toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}
                  </Typography>
                  <Button
                    size="small"
                    sx={{ color: 'text.secondary', minWidth: 0 }}
                    onClick={() => setOverrideHarvest(autoHarvestDate)}
                  >
                    Edit
                  </Button>
                </Stack>
              )}

              {(overrideHarvest || (!autoHarvestDate && selectedPlant)) && (
                <TextField
                  label="Expected harvest date"
                  type="date"
                  value={overrideHarvest}
                  onChange={e => setOverrideHarvest(e.target.value)}
                  helperText="Optional"
                  slotProps={{ inputLabel: { shrink: true } }}
                />
              )}

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

              <Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                  Planting type
                </Typography>
                <Stack direction="row" sx={{ gap: 1 }}>
                  {PLANTING_TYPES.map(opt => {
                    const selected = plantingType === opt.value
                    return (
                      <Chip
                        key={opt.value}
                        label={opt.label}
                        onClick={() => setPlantingType(opt.value)}
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

              {mutation.isError && (
                <Typography variant="body2" color="error">
                  Something went wrong. Please try again.
                </Typography>
              )}

              <Stack direction="row" sx={{ gap: 2, justifyContent: 'flex-end', mt: 1 }}>
                <Button onClick={() => navigate(`/gardens/${gardenId}/beds/${bedId}`)} color="inherit">
                  Cancel
                </Button>
                <Button
                  type="submit"
                  variant="contained"
                  color="secondary"
                  disabled={mutation.isPending}
                >
                  {mutation.isPending ? 'Adding…' : 'Add planting'}
                </Button>
              </Stack>
            </Stack>
          </form>
        </Card>
      </Container>
    </Box>
  )
}
