import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  Grid,
  IconButton,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined'
import YardOutlinedIcon from '@mui/icons-material/YardOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getGardenBed, getPlantings, deleteGardenBed, type PlantingSummary } from '../api/gardens'
import { AppHeader } from '../components/layout/AppHeader'
import { ConfirmDeleteDialog } from '../components/layout/ConfirmDeleteDialog'
import { SoilTestLog } from '../components/beds/SoilTestLog'
import { AmendmentLog } from '../components/beds/AmendmentLog'
import { PestDiseaseLog } from '../components/beds/PestDiseaseLog'
import { useState } from 'react'

function statusChipSx(status: string) {
  switch (status) {
    case 'Planted':
      return { bgcolor: 'primary.main', color: '#fff' }
    case 'Growing':
      return { bgcolor: '#8BA47A', color: '#fff' }
    case 'Producing':
      return { bgcolor: 'secondary.main', color: '#fff' }
    case 'Harvested':
      return { bgcolor: 'background.paper', color: 'text.secondary', border: '1px solid #C8C5BE' }
    case 'Failed':
      return { bgcolor: 'error.main', color: '#fff' }
    default:
      return { bgcolor: 'background.paper', color: 'text.secondary' }
  }
}

function formatDate(iso: string) {
  return new Date(iso + 'T00:00:00').toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function PlantingCard({ planting, gardenId }: { planting: PlantingSummary; gardenId: string }) {
  const navigate = useNavigate()
  return (
    <Card
      sx={{ p: 2.5, cursor: 'pointer', '&:hover': { boxShadow: '0 4px 16px rgba(44,44,40,0.12)' } }}
      onClick={() => navigate(`/gardens/${gardenId}/plantings/${planting.id}`)}
    >
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
        <Typography variant="h6" sx={{ fontStyle: 'italic', lineHeight: 1.2, fontFamily: '"Spectral", serif' }}>
          {planting.plantCommonName}
        </Typography>
        <Chip label={planting.status} size="small" sx={statusChipSx(planting.status)} />
      </Stack>
      <Typography variant="body2" color="text.secondary">
        Planted {formatDate(planting.plantedDate)}
        {planting.expectedHarvestDate && ` · Harvest ~${formatDate(planting.expectedHarvestDate)}`}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
        {planting.quantity} {planting.quantity === 1 ? 'plant' : 'plants'} · {planting.plantingType}
      </Typography>
    </Card>
  )
}

function EmptyPlantings({ onAdd }: { onAdd: () => void }) {
  return (
    <Card sx={{ p: 4, textAlign: 'center' }}>
      <YardOutlinedIcon sx={{ fontSize: 40, color: 'primary.main', mb: 2, opacity: 0.5 }} />
      <Typography variant="h6" sx={{ mb: 0.5 }}>
        Nothing planted yet
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Add a planting to start tracking what's growing here.
      </Typography>
      <Button variant="contained" color="secondary" startIcon={<AddOutlinedIcon />} onClick={onAdd}>
        Add planting
      </Button>
    </Card>
  )
}

function dimensionLabel(bed: {
  lengthFeet: number | null
  widthFeet: number | null
  diameterFeet: number | null
  depthInches: number | null
  volumeGallons: number | null
}) {
  const parts: string[] = []
  if (bed.diameterFeet) parts.push(`${bed.diameterFeet} ft diameter`)
  else if (bed.lengthFeet && bed.widthFeet) parts.push(`${bed.lengthFeet} × ${bed.widthFeet} ft`)
  else if (bed.lengthFeet) parts.push(`${bed.lengthFeet} ft`)
  if (bed.depthInches) parts.push(`${bed.depthInches}" deep`)
  if (bed.volumeGallons) parts.push(`${bed.volumeGallons} gal`)
  return parts.join(' · ')
}

export function GardenBedDetailPage() {
  const { id: gardenId, bedId } = useParams<{ id: string; bedId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleteError, setDeleteError] = useState('')

  const { data: bed, isLoading: bedLoading, isError: bedError } = useQuery({
    queryKey: ['bed', gardenId, bedId],
    queryFn: () => getGardenBed(gardenId!, bedId!),
    enabled: !!gardenId && !!bedId,
  })

  const { data: plantings = [], isLoading: plantingsLoading } = useQuery({
    queryKey: ['plantings', gardenId, bedId],
    queryFn: () => getPlantings(gardenId!, bedId!),
    enabled: !!gardenId && !!bedId,
  })

  const deleteMutation = useMutation({
    mutationFn: () => deleteGardenBed(gardenId!, bedId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['garden', gardenId] })
      navigate(`/gardens/${gardenId}`)
    },
    onError: (err: unknown) => {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        'Could not delete this bed. Make sure there are no active plantings first.'
      setDeleteError(msg)
    },
  })

  const isLoading = bedLoading || plantingsLoading
  const addPlantingPath = `/gardens/${gardenId}/beds/${bedId}/plantings/new`
  const dims = bed ? dimensionLabel(bed) : ''

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppHeader />
      <Container maxWidth="lg" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/gardens/${gardenId}`)}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back to garden
        </Button>

        {isLoading && (
          <Stack sx={{ gap: 2 }}>
            <Skeleton variant="text" width="35%" height={48} />
            <Skeleton variant="text" width="50%" />
            <Skeleton variant="rectangular" height={140} sx={{ borderRadius: 2, mt: 2 }} />
          </Stack>
        )}

        {bedError && (
          <Typography color="error">Could not load bed.</Typography>
        )}

        {bed && (
          <>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 0.5, gap: 2 }}>
              <Typography variant="h4">{bed.name}</Typography>
              <Stack direction="row" sx={{ gap: 0.5, alignItems: 'center', flexShrink: 0 }}>
                <IconButton
                  size="small"
                  onClick={() => navigate(`/gardens/${gardenId}/beds/${bedId}/edit`)}
                  sx={{ color: 'text.secondary' }}
                  aria-label="Edit bed"
                >
                  <EditOutlinedIcon sx={{ fontSize: 18 }} />
                </IconButton>
                <IconButton
                  size="small"
                  onClick={() => { setDeleteError(''); setDeleteOpen(true) }}
                  sx={{ color: 'text.secondary' }}
                  aria-label="Delete bed"
                >
                  <DeleteOutlineOutlinedIcon sx={{ fontSize: 18 }} />
                </IconButton>
              </Stack>
            </Stack>

            <Typography variant="body1" color="text.secondary" sx={{ mb: dims ? 0.25 : 0 }}>
              {bed.type} · {bed.shape} · {bed.sunExposure}
            </Typography>
            {dims && (
              <Typography variant="body2" color="text.secondary" sx={{ mb: 0 }}>
                {dims}
              </Typography>
            )}
            {bed.soilType && (
              <Typography variant="body2" color="text.secondary">
                Soil: {bed.soilType}
              </Typography>
            )}

            <Stack sx={{ mt: 4, mb: 2, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="h5">Plantings</Typography>
              {plantings.length > 0 && (
                <Button
                  variant="outlined"
                  color="primary"
                  size="small"
                  startIcon={<AddOutlinedIcon />}
                  onClick={() => navigate(addPlantingPath)}
                >
                  Add planting
                </Button>
              )}
            </Stack>

            {plantings.length === 0 ? (
              <EmptyPlantings onAdd={() => navigate(addPlantingPath)} />
            ) : (
              <Grid container spacing={3}>
                {plantings.map(p => (
                  <Grid key={p.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                    <PlantingCard planting={p} gardenId={gardenId!} />
                  </Grid>
                ))}
              </Grid>
            )}

            <Stack sx={{ gap: 3, mt: 4 }}>
              <SoilTestLog gardenId={gardenId!} bedId={bedId!} />
              <AmendmentLog gardenId={gardenId!} bedId={bedId!} />
              <PestDiseaseLog gardenId={gardenId!} bedId={bedId!} />
            </Stack>
          </>
        )}
      </Container>

      <ConfirmDeleteDialog
        open={deleteOpen}
        title="Delete bed?"
        message={
          deleteError ||
          `"${bed?.name}" will be permanently deleted. This cannot be undone.`
        }
        loading={deleteMutation.isPending}
        onConfirm={() => {
          setDeleteError('')
          deleteMutation.mutate()
        }}
        onCancel={() => { setDeleteOpen(false); setDeleteError('') }}
      />
    </Box>
  )
}
