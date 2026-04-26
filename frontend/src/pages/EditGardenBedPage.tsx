import { useState, useEffect, type FormEvent } from 'react'
import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  Grid,
  Skeleton,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getGardenBed, updateGardenBed } from '../api/gardens'
import { AppHeader } from '../components/layout/AppHeader'

const BED_TYPES = [
  { value: 'InGround', label: 'In Ground' },
  { value: 'RaisedGround', label: 'Raised (Ground Level)' },
  { value: 'RaisedSupported', label: 'Raised (Supported)' },
  { value: 'Container', label: 'Container' },
  { value: 'VerticalPlanter', label: 'Vertical Planter' },
  { value: 'StrawBale', label: 'Straw Bale' },
]

const BED_SHAPES = [
  { value: 'Rectangle', label: 'Rectangle' },
  { value: 'Square', label: 'Square' },
  { value: 'Round', label: 'Round' },
  { value: 'Oval', label: 'Oval' },
  { value: 'LShaped', label: 'L-Shaped' },
  { value: 'Triangle', label: 'Triangle' },
  { value: 'FreeForm', label: 'Free Form' },
]

const SUN_OPTIONS = [
  { value: 'FullSun', label: 'Full Sun' },
  { value: 'PartialShade', label: 'Partial Shade' },
  { value: 'FullShade', label: 'Full Shade' },
]

function ChipGroup({
  label,
  options,
  value,
  onChange,
  error,
}: {
  label: string
  options: { value: string; label: string }[]
  value: string
  onChange: (v: string) => void
  error?: string
}) {
  return (
    <Box>
      <Typography variant="body2" color={error ? 'error' : 'text.secondary'} sx={{ mb: 1.5 }}>
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
      {error && (
        <Typography variant="caption" color="error" sx={{ mt: 0.5, display: 'block' }}>
          {error}
        </Typography>
      )}
    </Box>
  )
}

function parseDimension(val: string): number | null {
  const n = parseFloat(val)
  return isNaN(n) || n <= 0 ? null : n
}

export function EditGardenBedPage() {
  const { id: gardenId, bedId } = useParams<{ id: string; bedId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: bed, isLoading } = useQuery({
    queryKey: ['bed', gardenId, bedId],
    queryFn: () => getGardenBed(gardenId!, bedId!),
    enabled: !!gardenId && !!bedId,
  })

  const [name, setName] = useState('')
  const [type, setType] = useState('')
  const [shape, setShape] = useState('')
  const [sunExposure, setSunExposure] = useState('')
  const [lengthFeet, setLengthFeet] = useState('')
  const [widthFeet, setWidthFeet] = useState('')
  const [diameterFeet, setDiameterFeet] = useState('')
  const [depthInches, setDepthInches] = useState('')
  const [volumeGallons, setVolumeGallons] = useState('')
  const [soilType, setSoilType] = useState('')
  const [notes, setNotes] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [initialized, setInitialized] = useState(false)

  useEffect(() => {
    if (bed && !initialized) {
      setName(bed.name)
      setType(bed.type)
      setShape(bed.shape)
      setSunExposure(bed.sunExposure)
      setLengthFeet(bed.lengthFeet?.toString() ?? '')
      setWidthFeet(bed.widthFeet?.toString() ?? '')
      setDiameterFeet(bed.diameterFeet?.toString() ?? '')
      setDepthInches(bed.depthInches?.toString() ?? '')
      setVolumeGallons(bed.volumeGallons?.toString() ?? '')
      setSoilType(bed.soilType ?? '')
      setNotes(bed.notes ?? '')
      setInitialized(true)
    }
  }, [bed, initialized])

  const mutation = useMutation({
    mutationFn: () =>
      updateGardenBed(gardenId!, bedId!, {
        name: name.trim(),
        type,
        shape,
        sunExposure,
        lengthFeet: parseDimension(lengthFeet),
        widthFeet: parseDimension(widthFeet),
        diameterFeet: parseDimension(diameterFeet),
        depthInches: parseDimension(depthInches),
        volumeGallons: parseDimension(volumeGallons),
        soilType: soilType.trim() || null,
        notes: notes.trim() || null,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['bed', gardenId, bedId], updated)
      queryClient.invalidateQueries({ queryKey: ['garden', gardenId] })
      navigate(`/gardens/${gardenId}/beds/${bedId}`)
    },
  })

  function validate() {
    const errs: Record<string, string> = {}
    if (!name.trim()) errs.name = 'Bed name is required.'
    if (!type) errs.type = 'Select a bed type.'
    if (!shape) errs.shape = 'Select a shape.'
    if (!sunExposure) errs.sunExposure = 'Select a sun exposure.'
    return errs
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const errs = validate()
    if (Object.keys(errs).length > 0) {
      setErrors(errs)
      return
    }
    setErrors({})
    mutation.mutate()
  }

  const isRound = shape === 'Round' || shape === 'Oval'
  const isContainer = type === 'Container'

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppHeader />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/gardens/${gardenId}/beds/${bedId}`)}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back to bed
        </Button>

        <Typography variant="h4" sx={{ mb: 4 }}>
          Edit Bed
        </Typography>

        {isLoading && (
          <Stack sx={{ gap: 3 }}>
            <Skeleton variant="rounded" height={56} />
            <Skeleton variant="rounded" height={60} />
            <Skeleton variant="rounded" height={60} />
          </Stack>
        )}

        {!isLoading && bed && (
          <Card sx={{ p: 4 }}>
            <form onSubmit={handleSubmit} noValidate>
              <Stack sx={{ gap: 3 }}>
                <TextField
                  label="Bed name"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  autoFocus
                  required
                  error={!!errors.name}
                  helperText={errors.name}
                />

                <ChipGroup
                  label="Bed type"
                  options={BED_TYPES}
                  value={type}
                  onChange={v => setType(v)}
                  error={errors.type}
                />

                <ChipGroup
                  label="Shape"
                  options={BED_SHAPES}
                  value={shape}
                  onChange={v => setShape(v)}
                  error={errors.shape}
                />

                <ChipGroup
                  label="Sun exposure"
                  options={SUN_OPTIONS}
                  value={sunExposure}
                  onChange={v => setSunExposure(v)}
                  error={errors.sunExposure}
                />

                <Box>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                    Dimensions (optional)
                  </Typography>
                  <Grid container spacing={2}>
                    {isRound ? (
                      <Grid size={{ xs: 6 }}>
                        <TextField
                          label="Diameter (ft)"
                          value={diameterFeet}
                          onChange={e => setDiameterFeet(e.target.value)}
                          type="number"
                          slotProps={{ htmlInput: { min: 0, step: 0.5 } }}
                        />
                      </Grid>
                    ) : (
                      <>
                        <Grid size={{ xs: 6 }}>
                          <TextField
                            label="Length (ft)"
                            value={lengthFeet}
                            onChange={e => setLengthFeet(e.target.value)}
                            type="number"
                            slotProps={{ htmlInput: { min: 0, step: 0.5 } }}
                          />
                        </Grid>
                        <Grid size={{ xs: 6 }}>
                          <TextField
                            label="Width (ft)"
                            value={widthFeet}
                            onChange={e => setWidthFeet(e.target.value)}
                            type="number"
                            slotProps={{ htmlInput: { min: 0, step: 0.5 } }}
                          />
                        </Grid>
                      </>
                    )}
                    {isContainer ? (
                      <Grid size={{ xs: 6 }}>
                        <TextField
                          label="Volume (gal)"
                          value={volumeGallons}
                          onChange={e => setVolumeGallons(e.target.value)}
                          type="number"
                          slotProps={{ htmlInput: { min: 0, step: 1 } }}
                        />
                      </Grid>
                    ) : (
                      <Grid size={{ xs: 6 }}>
                        <TextField
                          label="Depth (in)"
                          value={depthInches}
                          onChange={e => setDepthInches(e.target.value)}
                          type="number"
                          slotProps={{ htmlInput: { min: 0, step: 1 } }}
                        />
                      </Grid>
                    )}
                  </Grid>
                </Box>

                <TextField
                  label="Soil type"
                  value={soilType}
                  onChange={e => setSoilType(e.target.value)}
                  helperText="Optional — e.g., loam, sandy, clay"
                />

                <TextField
                  label="Notes"
                  value={notes}
                  onChange={e => setNotes(e.target.value)}
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
                  <Button onClick={() => navigate(`/gardens/${gardenId}/beds/${bedId}`)} color="inherit">
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
