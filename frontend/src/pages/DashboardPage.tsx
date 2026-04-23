import {
  Box,
  Button,
  Card,
  Container,
  Grid,
  Skeleton,
  Stack,
  Typography,
} from '@mui/material'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import YardOutlinedIcon from '@mui/icons-material/YardOutlined'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { getGardens } from '../api/gardens'
import { getInsights, markInsightRead, type UserInsight } from '../api/insights'
import { GardenCard } from '../components/gardens/GardenCard'
import { InsightChip } from '../components/insights/InsightChip'
import { WeatherStrip } from '../components/layout/WeatherStrip'
import { useAuth } from '../contexts/AuthContext'

function GardenGridSkeleton() {
  return (
    <Grid container spacing={3}>
      {[0, 1, 2].map(i => (
        <Grid key={i} size={{ xs: 12, sm: 6, lg: 4 }}>
          <Card sx={{ p: 2.5 }}>
            <Skeleton variant="text" width="60%" height={32} />
            <Skeleton variant="text" width="80%" sx={{ mt: 1 }} />
            <Skeleton variant="text" width="40%" sx={{ mt: 1 }} />
          </Card>
        </Grid>
      ))}
    </Grid>
  )
}

function EmptyGardens() {
  const navigate = useNavigate()
  return (
    <Card sx={{ p: 5, textAlign: 'center' }}>
      <YardOutlinedIcon sx={{ fontSize: 48, color: 'primary.main', mb: 2, opacity: 0.6 }} />
      <Typography variant="h5" sx={{ mb: 1 }}>
        No gardens yet
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Create your first garden to start tracking plantings, tasks, and harvests.
      </Typography>
      <Button
        variant="contained"
        color="secondary"
        startIcon={<AddOutlinedIcon />}
        onClick={() => navigate('/gardens/new')}
      >
        Create a Garden
      </Button>
    </Card>
  )
}

function NeedsAttentionSection({ householdId }: { householdId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ['insights', householdId]

  const { data: insights = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getInsights(householdId),
  })

  const dismiss = useMutation({
    mutationFn: (insightId: string) => markInsightRead(householdId, insightId),
    onMutate: async (insightId) => {
      await queryClient.cancelQueries({ queryKey })
      const prev = queryClient.getQueryData<UserInsight[]>(queryKey)
      queryClient.setQueryData<UserInsight[]>(queryKey, old =>
        (old ?? []).filter(i => i.id !== insightId)
      )
      return { prev }
    },
    onError: (_err, _id, ctx) => {
      if (ctx?.prev) queryClient.setQueryData(queryKey, ctx.prev)
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey }),
  })

  if (isLoading) {
    return (
      <Box sx={{ mt: 5 }}>
        <Skeleton variant="text" width={160} height={28} sx={{ mb: 2 }} />
        <Stack direction="row" sx={{ gap: 1.5 }}>
          {[0, 1, 2].map(i => <Skeleton key={i} variant="rounded" width={160} height={32} />)}
        </Stack>
      </Box>
    )
  }

  if (insights.length === 0) return null

  return (
    <Box sx={{ mt: 5 }}>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Needs Attention
      </Typography>
      <Box
        sx={{
          display: 'flex',
          flexWrap: { xs: 'nowrap', sm: 'wrap' },
          overflowX: { xs: 'auto', sm: 'visible' },
          gap: 1.5,
          pb: { xs: 1, sm: 0 },
        }}
      >
        {insights.map(insight => (
          <InsightChip
            key={insight.id}
            insight={insight}
            onDismiss={() => dismiss.mutate(insight.id)}
          />
        ))}
      </Box>
    </Box>
  )
}

export function DashboardPage() {
  const navigate = useNavigate()
  const { user } = useAuth()

  const { data: gardens = [], isLoading, isError } = useQuery({
    queryKey: ['gardens'],
    queryFn: () => getGardens(),
  })

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <WeatherStrip />

      <Container maxWidth="lg" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Stack
          direction="row"
          sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 3 }}
        >
          <Typography variant="h4">Your Gardens</Typography>
          <Button
            variant="contained"
            color="secondary"
            startIcon={<AddOutlinedIcon />}
            onClick={() => navigate('/gardens/new')}
            size="small"
          >
            New Garden
          </Button>
        </Stack>

        {isLoading && <GardenGridSkeleton />}

        {isError && (
          <Typography color="error" variant="body2">
            Could not load gardens. Make sure you&apos;re signed in.
          </Typography>
        )}

        {!isLoading && !isError && gardens.length === 0 && <EmptyGardens />}

        {!isLoading && !isError && gardens.length > 0 && (
          <Grid container spacing={3}>
            {gardens.map(g => (
              <Grid key={g.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                <GardenCard garden={g} />
              </Grid>
            ))}
          </Grid>
        )}

        {user?.householdId && <NeedsAttentionSection householdId={user.householdId} />}
      </Container>
    </Box>
  )
}
