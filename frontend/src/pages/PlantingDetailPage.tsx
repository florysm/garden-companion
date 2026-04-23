import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  Divider,
  IconButton,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getPlanting, updatePlantingStatus, deletePlanting } from '../api/gardens'
import { WeatherStrip } from '../components/layout/WeatherStrip'
import { ConfirmDeleteDialog } from '../components/layout/ConfirmDeleteDialog'
import { ObservationLog } from '../components/plantings/ObservationLog'
import { HarvestLog } from '../components/plantings/HarvestLog'
import { useState } from 'react'

// Status display config
const STATUS_CONFIG: Record<string, { label: string; sx: object }> = {
  Planted:   { label: 'Planted',   sx: { bgcolor: 'primary.main', color: '#fff' } },
  Growing:   { label: 'Growing',   sx: { bgcolor: '#8BA47A', color: '#fff' } },
  Producing: { label: 'Producing', sx: { bgcolor: 'secondary.main', color: '#fff' } },
  Harvested: { label: 'Harvested', sx: { bgcolor: 'background.paper', color: 'text.secondary', border: '1px solid #C8C5BE' } },
  Failed:    { label: 'Failed',    sx: { bgcolor: 'error.main', color: '#fff' } },
}

// What statuses can follow the current one
const STATUS_PROGRESSION: Record<string, string[]> = {
  Planted:   ['Growing'],
  Growing:   ['Producing'],
  Producing: ['Harvested'],
  Harvested: [],
  Failed:    [],
}

const TERMINAL = new Set(['Harvested', 'Failed'])

function formatDate(iso: string) {
  return new Date(iso + 'T00:00:00').toLocaleDateString(undefined, {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })
}

function StatCard({ label, value }: { label: string; value: string | number }) {
  return (
    <Box sx={{ flex: 1, bgcolor: 'background.default', borderRadius: 2, p: 2, textAlign: 'center' }}>
      <Typography variant="h5" sx={{ fontFamily: '"Spectral", serif', mb: 0.25 }}>
        {value}
      </Typography>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
    </Box>
  )
}

export function PlantingDetailPage() {
  const { id: gardenId, plantingId } = useParams<{ id: string; plantingId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [deleteOpen, setDeleteOpen] = useState(false)

  const { data: planting, isLoading, isError } = useQuery({
    queryKey: ['planting', plantingId],
    queryFn: () => getPlanting(plantingId!),
    enabled: !!plantingId,
  })

  const statusMutation = useMutation({
    mutationFn: (status: string) => updatePlantingStatus(plantingId!, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['planting', plantingId] })
      queryClient.invalidateQueries({ queryKey: ['plantings', gardenId] })
      queryClient.invalidateQueries({ queryKey: ['bed', gardenId, planting?.gardenBedId] })
      queryClient.invalidateQueries({ queryKey: ['garden', gardenId] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => deletePlanting(plantingId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['plantings', gardenId, planting?.gardenBedId] })
      queryClient.invalidateQueries({ queryKey: ['bed', gardenId, planting?.gardenBedId] })
      navigate(`/gardens/${gardenId}/beds/${planting?.gardenBedId}`)
    },
  })

  const status = planting?.status ?? ''
  const nextStatuses = STATUS_PROGRESSION[status] ?? []
  const isTerminal = TERMINAL.has(status)
  const cfg = STATUS_CONFIG[status]

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <WeatherStrip />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() =>
            planting
              ? navigate(`/gardens/${planting.gardenId}/beds/${planting.gardenBedId}`)
              : navigate('/')
          }
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back to bed
        </Button>

        {isLoading && (
          <Stack sx={{ gap: 2 }}>
            <Skeleton variant="text" width="60%" height={48} />
            <Skeleton variant="text" width="35%" />
            <Skeleton variant="rectangular" height={200} sx={{ borderRadius: 2, mt: 2 }} />
          </Stack>
        )}

        {isError && (
          <Typography color="error">Could not load planting.</Typography>
        )}

        {planting && (
          <Stack sx={{ gap: 3 }}>
            {/* Header */}
            <Box>
              <Stack direction="row" sx={{ alignItems: 'flex-start', justifyContent: 'space-between', gap: 2, mb: 0.5 }}>
                <Typography variant="h4" sx={{ fontStyle: 'italic' }}>
                  {planting.plantCommonName}
                </Typography>
                <Stack direction="row" sx={{ alignItems: 'center', gap: 0.5, flexShrink: 0 }}>
                  {cfg && <Chip label={cfg.label} size="small" sx={cfg.sx} />}
                  <IconButton
                    size="small"
                    onClick={() => navigate(`/gardens/${gardenId}/plantings/${plantingId}/edit`)}
                    sx={{ color: 'text.secondary' }}
                    aria-label="Edit planting"
                  >
                    <EditOutlinedIcon sx={{ fontSize: 18 }} />
                  </IconButton>
                  <IconButton
                    size="small"
                    onClick={() => setDeleteOpen(true)}
                    sx={{ color: 'text.secondary' }}
                    aria-label="Delete planting"
                  >
                    <DeleteOutlineOutlinedIcon sx={{ fontSize: 18 }} />
                  </IconButton>
                </Stack>
              </Stack>
              {planting.plantScientificName && (
                <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                  {planting.plantScientificName}
                  {planting.plantFamily ? ` · ${planting.plantFamily}` : ''}
                </Typography>
              )}
            </Box>

            {/* Details card */}
            <Card sx={{ p: 3 }}>
              <Stack sx={{ gap: 0 }}>
                <DetailRow label="Planted" value={formatDate(planting.plantedDate)} />
                {planting.expectedHarvestDate && (
                  <>
                    <Divider sx={{ my: 1.5 }} />
                    <DetailRow
                      label="Expected harvest"
                      value={formatDate(planting.expectedHarvestDate)}
                    />
                  </>
                )}
                {planting.actualEndDate && (
                  <>
                    <Divider sx={{ my: 1.5 }} />
                    <DetailRow
                      label={planting.status === 'Harvested' ? 'Harvested on' : 'Ended on'}
                      value={formatDate(planting.actualEndDate)}
                    />
                  </>
                )}
                <Divider sx={{ my: 1.5 }} />
                <DetailRow label="Quantity" value={`${planting.quantity} ${planting.quantity === 1 ? 'plant' : 'plants'}`} />
                <Divider sx={{ my: 1.5 }} />
                <DetailRow label="Type" value={planting.plantingType} />
                <Divider sx={{ my: 1.5 }} />
                <DetailRow label="Season" value={`${planting.seasonType} ${planting.seasonYear}`} />
                <Divider sx={{ my: 1.5 }} />
                <DetailRow label="Bed" value={planting.gardenBedName} />
              </Stack>
            </Card>

            {/* Stats */}
            <Stack direction="row" sx={{ gap: 2 }}>
              <StatCard label="observations" value={planting.observationCount} />
              <StatCard label="harvests logged" value={planting.harvestCount} />
            </Stack>

            {/* Observation log */}
            <ObservationLog plantingId={plantingId!} />

            {/* Harvest log */}
            <HarvestLog plantingId={plantingId!} />

            {/* Status update */}
            {!isTerminal && (
              <Card sx={{ p: 3 }}>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  Update status
                </Typography>
                <Stack sx={{ gap: 1.5 }}>
                  {nextStatuses.map(next => (
                    <Button
                      key={next}
                      variant="contained"
                      color="secondary"
                      fullWidth
                      disabled={statusMutation.isPending}
                      onClick={() => statusMutation.mutate(next)}
                    >
                      Mark as {STATUS_CONFIG[next]?.label ?? next}
                    </Button>
                  ))}
                  <Button
                    variant="outlined"
                    fullWidth
                    disabled={statusMutation.isPending}
                    onClick={() => statusMutation.mutate('Failed')}
                    sx={{ color: 'error.main', borderColor: 'error.main', '&:hover': { borderColor: 'error.dark', bgcolor: 'transparent' } }}
                  >
                    Mark as failed
                  </Button>
                </Stack>
                {statusMutation.isError && (
                  <Typography variant="caption" color="error" sx={{ mt: 1, display: 'block' }}>
                    Could not update status. Please try again.
                  </Typography>
                )}
              </Card>
            )}
          </Stack>
        )}
      </Container>

      <ConfirmDeleteDialog
        open={deleteOpen}
        title="Delete planting?"
        message={`This planting of ${planting?.plantCommonName} will be permanently removed. This cannot be undone.`}
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setDeleteOpen(false)}
      />
    </Box>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
      <Typography variant="body2" color="text.secondary" sx={{ flexShrink: 0 }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ textAlign: 'right' }}>
        {value}
      </Typography>
    </Stack>
  )
}
