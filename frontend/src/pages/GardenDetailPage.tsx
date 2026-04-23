import {
  Box,
  Button,
  Card,
  Chip,
  Container,
  Divider,
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
import WbSunnyOutlinedIcon from '@mui/icons-material/WbSunnyOutlined'
import RadioButtonUncheckedIcon from '@mui/icons-material/RadioButtonUnchecked'
import WaterDropOutlinedIcon from '@mui/icons-material/WaterDropOutlined'
import ScienceOutlinedIcon from '@mui/icons-material/ScienceOutlined'
import AgricultureOutlinedIcon from '@mui/icons-material/AgricultureOutlined'
import ContentCutOutlinedIcon from '@mui/icons-material/ContentCutOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import LayersOutlinedIcon from '@mui/icons-material/LayersOutlined'
import AssignmentOutlinedIcon from '@mui/icons-material/AssignmentOutlined'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getGarden, deleteGarden, type GardenBedSummary } from '../api/gardens'
import { getGardenTasks, completeGardenTask, deleteGardenTask, type GardenTask } from '../api/tasks'
import { WeatherStrip } from '../components/layout/WeatherStrip'
import { ConfirmDeleteDialog } from '../components/layout/ConfirmDeleteDialog'
import { useState } from 'react'
import type { SvgIconComponent } from '@mui/icons-material'

// ── Task type display config ──────────────────────────────────────────────────

interface TaskTypeConfig { Icon: SvgIconComponent; color: string }

const TASK_TYPE_CONFIG: Record<string, TaskTypeConfig> = {
  Water:     { Icon: WaterDropOutlinedIcon,   color: '#3D6B7A' },
  Fertilize: { Icon: ScienceOutlinedIcon,     color: '#6B7F5E' },
  Harvest:   { Icon: AgricultureOutlinedIcon, color: '#8B6914' },
  Prune:     { Icon: ContentCutOutlinedIcon,  color: '#7A786F' },
  Inspect:   { Icon: VisibilityOutlinedIcon,  color: '#6B7F5E' },
  Amend:     { Icon: LayersOutlinedIcon,      color: '#8B6914' },
  Plant:     { Icon: YardOutlinedIcon,        color: '#6B7F5E' },
  General:   { Icon: AssignmentOutlinedIcon,  color: '#7A786F' },
}

const DEFAULT_TASK_TYPE: TaskTypeConfig = { Icon: AssignmentOutlinedIcon, color: '#7A786F' }

// ── Bed card ─────────────────────────────────────────────────────────────────

function BedCard({ bed, gardenId }: { bed: GardenBedSummary; gardenId: string }) {
  const navigate = useNavigate()
  return (
    <Card
      sx={{ p: 2.5, cursor: 'pointer', '&:hover': { boxShadow: '0 4px 16px rgba(44,44,40,0.12)' } }}
      onClick={() => navigate(`/gardens/${gardenId}/beds/${bed.id}`)}
    >
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
        <Typography variant="h6" sx={{ lineHeight: 1.2 }}>
          {bed.name}
        </Typography>
        <WbSunnyOutlinedIcon sx={{ fontSize: 18, color: 'text.secondary', mt: 0.25 }} />
      </Stack>
      <Typography variant="body2" color="text.secondary">
        {bed.type} · {bed.sunExposure}
      </Typography>
      {bed.activePlantingCount > 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {bed.activePlantingCount} active {bed.activePlantingCount === 1 ? 'planting' : 'plantings'}
        </Typography>
      )}
    </Card>
  )
}

function EmptyBeds({ onAdd }: { onAdd: () => void }) {
  return (
    <Card sx={{ p: 4, textAlign: 'center' }}>
      <YardOutlinedIcon sx={{ fontSize: 40, color: 'primary.main', mb: 2, opacity: 0.5 }} />
      <Typography variant="h6" sx={{ mb: 0.5 }}>
        No beds yet
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Add a bed to start tracking plantings.
      </Typography>
      <Button variant="contained" color="secondary" startIcon={<AddOutlinedIcon />} onClick={onAdd}>
        Add a bed
      </Button>
    </Card>
  )
}

// ── Task row ─────────────────────────────────────────────────────────────────

function formatDueDate(iso: string) {
  return new Date(iso + 'T00:00:00').toLocaleDateString(undefined, {
    month: 'short', day: 'numeric', year: 'numeric',
  })
}

function TaskRow({
  task,
  gardenId,
  bedName,
  onComplete,
  onDelete,
}: {
  task: GardenTask
  gardenId: string
  bedName: string | null
  onComplete: () => void
  onDelete: () => void
}) {
  const navigate = useNavigate()
  const cfg = TASK_TYPE_CONFIG[task.taskType] ?? DEFAULT_TASK_TYPE
  const { Icon } = cfg
  const isCompleted = !!task.completedAt

  return (
    <Stack direction="row" sx={{ alignItems: 'center', px: 2.5, py: 1.5, gap: 1.5 }}>
      <Icon sx={{ fontSize: 18, color: cfg.color, flexShrink: 0, opacity: isCompleted ? 0.4 : 1 }} />

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography
          variant="body2"
          sx={{
            textDecoration: isCompleted ? 'line-through' : 'none',
            color: isCompleted ? 'text.secondary' : 'text.primary',
          }}
        >
          {task.title}
        </Typography>
        <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
          {!isCompleted && task.dueDate && (
            <Typography variant="caption" color={task.isOverdue ? 'error.main' : 'text.secondary'}>
              {task.isOverdue ? 'Overdue · ' : 'Due '}{formatDueDate(task.dueDate)}
            </Typography>
          )}
          {isCompleted && task.completedAt && (
            <Typography variant="caption" color="text.secondary">
              Completed {new Date(task.completedAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
            </Typography>
          )}
          {bedName && (
            <Typography variant="caption" color="text.secondary">
              · {bedName}
            </Typography>
          )}
          {task.assignedToDisplayName && (
            <Typography variant="caption" color="text.secondary">
              · {task.assignedToDisplayName}
            </Typography>
          )}
        </Stack>
      </Box>

      <Stack direction="row" sx={{ flexShrink: 0 }}>
        {!isCompleted && (
          <IconButton
            size="small"
            onClick={() => navigate(`/gardens/${gardenId}/tasks/${task.id}/edit`)}
            sx={{ color: 'text.secondary' }}
            aria-label="Edit task"
          >
            <EditOutlinedIcon sx={{ fontSize: 16 }} />
          </IconButton>
        )}
        {!isCompleted && (
          <IconButton size="small" onClick={onComplete} sx={{ color: 'text.secondary' }}>
            <RadioButtonUncheckedIcon sx={{ fontSize: 18 }} />
          </IconButton>
        )}
        <IconButton size="small" onClick={onDelete} sx={{ color: 'text.secondary' }}>
          <DeleteOutlineOutlinedIcon sx={{ fontSize: 18 }} />
        </IconButton>
      </Stack>
    </Stack>
  )
}

// ── Tasks section ─────────────────────────────────────────────────────────────

function TasksSection({ gardenId, beds }: { gardenId: string; beds: GardenBedSummary[] }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [showCompleted, setShowCompleted] = useState(false)

  const queryKey = ['tasks', gardenId, showCompleted]

  const { data: tasks = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getGardenTasks(gardenId, { isCompleted: showCompleted }),
  })

  const completeMutation = useMutation({
    mutationFn: (taskId: string) => completeGardenTask(gardenId, taskId),
    onMutate: async (taskId) => {
      await queryClient.cancelQueries({ queryKey })
      const prev = queryClient.getQueryData<GardenTask[]>(queryKey)
      queryClient.setQueryData<GardenTask[]>(queryKey, old => (old ?? []).filter(t => t.id !== taskId))
      return { prev }
    },
    onError: (_e, _id, ctx) => {
      if (ctx?.prev) queryClient.setQueryData(queryKey, ctx.prev)
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['tasks', gardenId] }),
  })

  const deleteMutation = useMutation({
    mutationFn: (taskId: string) => deleteGardenTask(gardenId, taskId),
    onMutate: async (taskId) => {
      await queryClient.cancelQueries({ queryKey })
      const prev = queryClient.getQueryData<GardenTask[]>(queryKey)
      queryClient.setQueryData<GardenTask[]>(queryKey, old => (old ?? []).filter(t => t.id !== taskId))
      return { prev }
    },
    onError: (_e, _id, ctx) => {
      if (ctx?.prev) queryClient.setQueryData(queryKey, ctx.prev)
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['tasks', gardenId] }),
  })

  const filterChipSx = (active: boolean) => ({
    bgcolor: active ? 'primary.main' : 'background.default',
    color: active ? '#fff' : 'text.secondary',
    border: '1.5px solid',
    borderColor: active ? 'primary.main' : '#C8C5BE',
    fontWeight: active ? 500 : 400,
    '&:hover': { bgcolor: active ? 'primary.dark' : '#E8E5DE' },
  })

  return (
    <Box sx={{ mt: 5 }}>
      {/* Section header */}
      <Stack direction="row" sx={{ alignItems: 'center', mb: 2, gap: 1.5, flexWrap: 'wrap' }}>
        <Typography variant="h5" sx={{ flexShrink: 0 }}>Tasks</Typography>
        <Stack direction="row" sx={{ gap: 0.75 }}>
          <Chip
            label="Open"
            size="small"
            onClick={() => setShowCompleted(false)}
            sx={filterChipSx(!showCompleted)}
          />
          <Chip
            label="Completed"
            size="small"
            onClick={() => setShowCompleted(true)}
            sx={filterChipSx(showCompleted)}
          />
        </Stack>
        <Box sx={{ flex: 1 }} />
        {!showCompleted && (
          <Button
            variant="outlined"
            color="primary"
            size="small"
            startIcon={<AddOutlinedIcon />}
            onClick={() => navigate(`/gardens/${gardenId}/tasks/new`)}
          >
            Add task
          </Button>
        )}
      </Stack>

      {/* Loading */}
      {isLoading && (
        <Card sx={{ p: 0, overflow: 'hidden' }}>
          {[0, 1, 2].map((i, idx) => (
            <Box key={i}>
              {idx > 0 && <Divider />}
              <Stack direction="row" sx={{ alignItems: 'center', px: 2.5, py: 1.5, gap: 1.5 }}>
                <Skeleton variant="circular" width={18} height={18} />
                <Box sx={{ flex: 1 }}>
                  <Skeleton variant="text" width="55%" />
                  <Skeleton variant="text" width="30%" height={14} />
                </Box>
              </Stack>
            </Box>
          ))}
        </Card>
      )}

      {/* Empty state */}
      {!isLoading && tasks.length === 0 && (
        <Card sx={{ p: 4, textAlign: 'center' }}>
          <AssignmentOutlinedIcon sx={{ fontSize: 36, color: 'primary.main', mb: 1.5, opacity: 0.5 }} />
          <Typography variant="h6" sx={{ mb: 0.5 }}>
            {showCompleted ? 'No completed tasks' : 'No open tasks'}
          </Typography>
          {!showCompleted && (
            <>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Add a task to start tracking what needs to get done.
              </Typography>
              <Button
                variant="contained"
                color="secondary"
                startIcon={<AddOutlinedIcon />}
                onClick={() => navigate(`/gardens/${gardenId}/tasks/new`)}
              >
                Add task
              </Button>
            </>
          )}
        </Card>
      )}

      {/* Task list */}
      {!isLoading && tasks.length > 0 && (
        <Card sx={{ p: 0, overflow: 'hidden' }}>
          {tasks.map((task, idx) => (
            <Box key={task.id}>
              {idx > 0 && <Divider />}
              <TaskRow
                task={task}
                gardenId={gardenId}
                bedName={task.gardenBedId ? (beds.find(b => b.id === task.gardenBedId)?.name ?? null) : null}
                onComplete={() => completeMutation.mutate(task.id)}
                onDelete={() => deleteMutation.mutate(task.id)}
              />
            </Box>
          ))}
        </Card>
      )}
    </Box>
  )
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function GardenDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [deleteOpen, setDeleteOpen] = useState(false)

  const { data: garden, isLoading, isError } = useQuery({
    queryKey: ['garden', id],
    queryFn: () => getGarden(id!),
    enabled: !!id,
  })

  const deleteMutation = useMutation({
    mutationFn: () => deleteGarden(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gardens'] })
      navigate('/')
    },
  })

  const isOwner = garden?.userRole === 'Owner'

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <WeatherStrip />
      <Container maxWidth="lg" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate('/')}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          All gardens
        </Button>

        {isLoading && (
          <Stack sx={{ gap: 2 }}>
            <Skeleton variant="text" width="40%" height={48} />
            <Skeleton variant="text" width="25%" />
            <Skeleton variant="rectangular" height={160} sx={{ borderRadius: 2, mt: 2 }} />
          </Stack>
        )}

        {isError && (
          <Typography color="error">Could not load garden.</Typography>
        )}

        {garden && (
          <>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 1, flexWrap: 'wrap', gap: 2 }}>
              <Box>
                <Typography variant="h4">{garden.name}</Typography>
                {garden.description && (
                  <Typography variant="body1" color="text.secondary" sx={{ mt: 0.5 }}>
                    {garden.description}
                  </Typography>
                )}
              </Box>
              <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap', alignItems: 'center' }}>
                {garden.types.map(t => (
                  <Chip
                    key={t}
                    label={t}
                    size="small"
                    sx={{ bgcolor: 'primary.main', color: '#fff', fontSize: '0.7rem' }}
                  />
                ))}
                <IconButton
                  size="small"
                  onClick={() => navigate(`/gardens/${id}/edit`)}
                  sx={{ color: 'text.secondary', ml: 0.5 }}
                  aria-label="Edit garden"
                >
                  <EditOutlinedIcon sx={{ fontSize: 18 }} />
                </IconButton>
                {isOwner && (
                  <IconButton
                    size="small"
                    onClick={() => setDeleteOpen(true)}
                    sx={{ color: 'text.secondary' }}
                    aria-label="Delete garden"
                  >
                    <DeleteOutlineOutlinedIcon sx={{ fontSize: 18 }} />
                  </IconButton>
                )}
              </Stack>
            </Stack>

            {/* Beds */}
            <Stack sx={{ mt: 4, mb: 2, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="h5">Beds</Typography>
              {garden.beds.length > 0 && (
                <Button
                  variant="outlined"
                  color="primary"
                  size="small"
                  startIcon={<AddOutlinedIcon />}
                  onClick={() => navigate(`/gardens/${id}/beds/new`)}
                >
                  Add bed
                </Button>
              )}
            </Stack>

            {garden.beds.length === 0 ? (
              <EmptyBeds onAdd={() => navigate(`/gardens/${id}/beds/new`)} />
            ) : (
              <Grid container spacing={3}>
                {garden.beds.map(bed => (
                  <Grid key={bed.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                    <BedCard bed={bed} gardenId={id!} />
                  </Grid>
                ))}
              </Grid>
            )}

            {/* Tasks */}
            <TasksSection gardenId={id!} beds={garden.beds} />
          </>
        )}
      </Container>

      <ConfirmDeleteDialog
        open={deleteOpen}
        title="Delete garden?"
        message={`"${garden?.name}" and all its beds and tasks will be permanently deleted. This cannot be undone.`}
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setDeleteOpen(false)}
      />
    </Box>
  )
}
