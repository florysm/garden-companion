import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  Divider,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import WbSunnyOutlinedIcon from '@mui/icons-material/WbSunnyOutlined'
import WaterDropOutlinedIcon from '@mui/icons-material/WaterDropOutlined'
import WhatshotOutlinedIcon from '@mui/icons-material/WhatshotOutlined'
import CheckCircleOutlineOutlinedIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getPlant, getPlantCompanions } from '../api/plants'
import { AppHeader } from '../components/layout/AppHeader'

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

export function PlantDetailPage() {
  const { plantId } = useParams<{ plantId: string }>()
  const navigate = useNavigate()

  const { data: plant, isLoading, isError } = useQuery({
    queryKey: ['plant', plantId],
    queryFn: () => getPlant(plantId!),
    enabled: !!plantId,
  })

  const { data: companions = [], isLoading: companionsLoading } = useQuery({
    queryKey: ['plant-companions', plantId],
    queryFn: () => getPlantCompanions(plantId!),
    enabled: !!plantId,
  })

  const beneficial = companions.filter(c => c.relationshipType === 'Beneficial')
  const harmful = companions.filter(c => c.relationshipType === 'Harmful')

  const hasGrowingInfo = plant && (
    plant.sunRequirement ||
    plant.waterRequirement ||
    plant.minSpacingInches ||
    plant.minDepthInches
  )

  const hasAnyFact = plant && (
    plant.daysToMaturity ||
    plant.heatLevelShu ||
    hasGrowingInfo
  )

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppHeader />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
          <Button
            startIcon={<ArrowBackOutlinedIcon />}
            onClick={() => navigate(-1)}
            sx={{ color: 'text.secondary', pl: 0 }}
          >
            Back
          </Button>
          {plant && !plant.isGlobal && (
            <Chip label="Custom plant" size="small" sx={{ bgcolor: 'background.paper', color: 'text.secondary', fontSize: '0.7rem' }} />
          )}
        </Stack>

        {isLoading && (
          <Stack sx={{ gap: 2 }}>
            <Skeleton variant="text" width="60%" height={48} />
            <Skeleton variant="text" width="40%" />
            <Skeleton variant="rectangular" height={180} sx={{ borderRadius: 2, mt: 2 }} />
          </Stack>
        )}

        {isError && (
          <Typography color="error">Could not load plant.</Typography>
        )}

        {plant && (
          <Stack sx={{ gap: 3 }}>
            {/* Header */}
            <Box>
              <Typography variant="h4" sx={{ fontStyle: 'italic', fontFamily: '"Spectral", serif' }}>
                {plant.commonName}
              </Typography>
              {plant.scientificName && (
                <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic', mt: 0.25 }}>
                  {plant.scientificName}
                  {plant.family ? ` · ${plant.family}` : ''}
                </Typography>
              )}
              {!plant.scientificName && plant.family && (
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                  {plant.family}
                </Typography>
              )}
            </Box>

            {/* Description */}
            {plant.description && (
              <Typography variant="body1" color="text.secondary" sx={{ lineHeight: 1.65 }}>
                {plant.description}
              </Typography>
            )}

            {/* Key facts */}
            <Card sx={{ p: 3 }}>
              <Stack sx={{ gap: 0 }}>
                {plant.daysToMaturity && (
                  <>
                    <DetailRow label="Days to maturity" value={`~${plant.daysToMaturity} days`} />
                    {(plant.heatLevelShu || hasGrowingInfo) && <Divider sx={{ my: 1.5 }} />}
                  </>
                )}
                {plant.heatLevelShu && (
                  <>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
                      <Stack direction="row" sx={{ alignItems: 'center', gap: 0.75 }}>
                        <WhatshotOutlinedIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Heat</Typography>
                      </Stack>
                      <Typography variant="body2">{plant.heatLevelShu.toLocaleString()} SHU</Typography>
                    </Stack>
                    {hasGrowingInfo && <Divider sx={{ my: 1.5 }} />}
                  </>
                )}
                {plant.sunRequirement && (
                  <>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
                      <Stack direction="row" sx={{ alignItems: 'center', gap: 0.75 }}>
                        <WbSunnyOutlinedIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Sun</Typography>
                      </Stack>
                      <Typography variant="body2">{plant.sunRequirement}</Typography>
                    </Stack>
                    {(plant.waterRequirement || plant.minSpacingInches || plant.minDepthInches) && (
                      <Divider sx={{ my: 1.5 }} />
                    )}
                  </>
                )}
                {plant.waterRequirement && (
                  <>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
                      <Stack direction="row" sx={{ alignItems: 'center', gap: 0.75 }}>
                        <WaterDropOutlinedIcon sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">Water</Typography>
                      </Stack>
                      <Typography variant="body2">{plant.waterRequirement}</Typography>
                    </Stack>
                    {(plant.minSpacingInches || plant.minDepthInches) && (
                      <Divider sx={{ my: 1.5 }} />
                    )}
                  </>
                )}
                {plant.minSpacingInches && (
                  <>
                    <DetailRow label="Min. spacing" value={`${plant.minSpacingInches}"`} />
                    {plant.minDepthInches && <Divider sx={{ my: 1.5 }} />}
                  </>
                )}
                {plant.minDepthInches && (
                  <DetailRow label="Planting depth" value={`${plant.minDepthInches}"`} />
                )}
                {!hasAnyFact && (
                  <Typography variant="body2" color="text.secondary">
                    No growing details recorded.
                  </Typography>
                )}
              </Stack>
            </Card>

            {/* Companions */}
            <Box>
              <Typography variant="h5" sx={{ mb: 2 }}>
                Companion Planting
              </Typography>

              {companionsLoading && (
                <Stack sx={{ gap: 1 }}>
                  <Skeleton variant="rounded" height={40} />
                  <Skeleton variant="rounded" height={40} />
                </Stack>
              )}

              {!companionsLoading && companions.length === 0 && (
                <Card sx={{ p: 3 }}>
                  <Typography variant="body2" color="text.secondary">
                    No companion planting relationships recorded.
                  </Typography>
                </Card>
              )}

              {!companionsLoading && companions.length > 0 && (
                <Stack sx={{ gap: 2 }}>
                  {beneficial.length > 0 && (
                    <Box>
                      <Stack direction="row" sx={{ alignItems: 'center', gap: 0.75, mb: 1.25 }}>
                        <CheckCircleOutlineOutlinedIcon sx={{ fontSize: 16, color: 'primary.main' }} />
                        <Typography variant="body2" sx={{ fontWeight: 500, color: 'primary.main' }}>
                          Grows well with
                        </Typography>
                      </Stack>
                      <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
                        {beneficial.map(c => (
                          <Chip
                            key={c.companionPlantId}
                            label={c.companionCommonName}
                            size="small"
                            onClick={() => navigate(`/plants/${c.companionPlantId}`)}
                            sx={{
                              bgcolor: '#EAF0E6',
                              color: 'primary.main',
                              fontStyle: c.companionScientificName ? 'italic' : 'normal',
                              cursor: 'pointer',
                              '&:hover': { bgcolor: '#DDE8D8' },
                            }}
                          />
                        ))}
                      </Stack>
                    </Box>
                  )}

                  {beneficial.length > 0 && harmful.length > 0 && (
                    <Divider />
                  )}

                  {harmful.length > 0 && (
                    <Box>
                      <Stack direction="row" sx={{ alignItems: 'center', gap: 0.75, mb: 1.25 }}>
                        <CancelOutlinedIcon sx={{ fontSize: 16, color: 'error.main' }} />
                        <Typography variant="body2" sx={{ fontWeight: 500, color: 'error.main' }}>
                          Keep away from
                        </Typography>
                      </Stack>
                      <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1 }}>
                        {harmful.map(c => (
                          <Chip
                            key={c.companionPlantId}
                            label={c.companionCommonName}
                            size="small"
                            onClick={() => navigate(`/plants/${c.companionPlantId}`)}
                            sx={{
                              bgcolor: '#FBEAEA',
                              color: 'error.main',
                              cursor: 'pointer',
                              '&:hover': { bgcolor: '#F5D5D5' },
                            }}
                          />
                        ))}
                      </Stack>
                    </Box>
                  )}
                </Stack>
              )}
            </Box>

            {/* Add custom plant CTA */}
            {!plant.isGlobal && (
              <Box sx={{ pt: 1 }}>
                <Divider sx={{ mb: 3 }} />
                <Button
                  variant="outlined"
                  color="primary"
                  startIcon={<AddOutlinedIcon />}
                  onClick={() => navigate('/plants/new')}
                  fullWidth
                >
                  Add another custom plant
                </Button>
              </Box>
            )}
          </Stack>
        )}
      </Container>
    </Box>
  )
}
