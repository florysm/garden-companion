import { Card, Stack, Typography, Chip } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import type { GardenSummary } from '../../api/gardens'

export function GardenCard({ garden }: { garden: GardenSummary }) {
  const navigate = useNavigate()

  return (
    <Card
      sx={{ p: 2.5, cursor: 'pointer', height: '100%', transition: 'box-shadow 0.15s' }}
      onClick={() => navigate(`/gardens/${garden.id}`)}
    >
      <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <Typography variant="h5" sx={{ lineHeight: 1.25 }}>
          {garden.name}
        </Typography>
        {garden.types.length > 0 && (
          <Stack direction="row" sx={{ gap: 0.5, flexShrink: 0, alignItems: 'center' }}>
            <Chip
              label={garden.types[0]}
              size="small"
              sx={{ bgcolor: 'primary.main', color: '#fff', fontSize: '0.7rem' }}
            />
            {garden.types.length > 1 && (
              <Chip
                label={`+${garden.types.length - 1}`}
                size="small"
                sx={{ bgcolor: 'background.default', color: 'text.secondary', fontSize: '0.7rem' }}
              />
            )}
          </Stack>
        )}
      </Stack>

      {garden.description && (
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ mt: 0.75, mb: 1, display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
        >
          {garden.description}
        </Typography>
      )}

      <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
        {garden.bedCount} {garden.bedCount === 1 ? 'bed' : 'beds'}
      </Typography>
    </Card>
  )
}
