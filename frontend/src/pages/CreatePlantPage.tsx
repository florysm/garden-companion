import { useState, type FormEvent } from 'react'
import {
  Box,
  Button,
  Card,
  Container,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { createPlant } from '../api/plants'
import { AppHeader } from '../components/layout/AppHeader'

export function CreatePlantPage() {
  const navigate = useNavigate()

  const [commonName, setCommonName] = useState('')
  const [scientificName, setScientificName] = useState('')
  const [family, setFamily] = useState('')
  const [description, setDescription] = useState('')
  const [daysToMaturity, setDaysToMaturity] = useState('')
  const [sunRequirement, setSunRequirement] = useState('')
  const [waterRequirement, setWaterRequirement] = useState('')
  const [minSpacingInches, setMinSpacingInches] = useState('')
  const [minDepthInches, setMinDepthInches] = useState('')
  const [nameError, setNameError] = useState('')

  const mutation = useMutation({
    mutationFn: () =>
      createPlant({
        commonName: commonName.trim(),
        scientificName: scientificName.trim() || undefined,
        family: family.trim() || undefined,
        description: description.trim() || undefined,
        daysToMaturity: daysToMaturity ? parseInt(daysToMaturity, 10) : undefined,
        sunRequirement: sunRequirement.trim() || undefined,
        waterRequirement: waterRequirement.trim() || undefined,
        minSpacingInches: minSpacingInches ? parseFloat(minSpacingInches) : undefined,
        minDepthInches: minDepthInches ? parseFloat(minDepthInches) : undefined,
      }),
    onSuccess: (plant) => {
      navigate(`/plants/${plant.id}`)
    },
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!commonName.trim()) {
      setNameError('Plant name is required.')
      return
    }
    setNameError('')
    mutation.mutate()
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppHeader />
      <Container maxWidth="sm" sx={{ py: 4, px: { xs: 2, sm: 3 } }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(-1)}
          sx={{ mb: 3, color: 'text.secondary', pl: 0 }}
        >
          Back
        </Button>

        <Typography variant="h4" sx={{ mb: 1 }}>
          Add Custom Plant
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
          Plants you add are private to your account until approved for the global catalog.
        </Typography>

        <Card sx={{ p: 4 }}>
          <form onSubmit={handleSubmit} noValidate>
            <Stack sx={{ gap: 3 }}>
              {/* Identity */}
              <TextField
                label="Common name"
                value={commonName}
                onChange={e => { setCommonName(e.target.value); setNameError('') }}
                required
                autoFocus
                error={!!nameError}
                helperText={nameError}
                slotProps={{ htmlInput: { maxLength: 200 } }}
              />

              <TextField
                label="Scientific name"
                value={scientificName}
                onChange={e => setScientificName(e.target.value)}
                helperText="Optional"
                slotProps={{ htmlInput: { maxLength: 200 } }}
                sx={{ '& input': { fontStyle: scientificName ? 'italic' : 'normal' } }}
              />

              <TextField
                label="Plant family"
                value={family}
                onChange={e => setFamily(e.target.value)}
                helperText="Optional — e.g., Solanaceae, Cucurbitaceae"
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />

              <TextField
                label="Description"
                value={description}
                onChange={e => setDescription(e.target.value)}
                multiline
                rows={3}
                helperText="Optional — growing notes, flavor, uses"
                slotProps={{ htmlInput: { maxLength: 2000 } }}
              />

              {/* Growing info */}
              <Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  Growing information (optional)
                </Typography>
                <Stack sx={{ gap: 2 }}>
                  <TextField
                    label="Days to maturity"
                    type="number"
                    value={daysToMaturity}
                    onChange={e => setDaysToMaturity(e.target.value)}
                    slotProps={{ htmlInput: { min: 1, max: 3650, step: 1 } }}
                    helperText="Approximate days from planting to harvest"
                  />

                  <TextField
                    label="Sun requirement"
                    value={sunRequirement}
                    onChange={e => setSunRequirement(e.target.value)}
                    helperText="e.g., Full Sun, Partial Shade, Full Shade"
                    slotProps={{ htmlInput: { maxLength: 100 } }}
                  />

                  <TextField
                    label="Water requirement"
                    value={waterRequirement}
                    onChange={e => setWaterRequirement(e.target.value)}
                    helperText="e.g., Low, Moderate, High"
                    slotProps={{ htmlInput: { maxLength: 100 } }}
                  />

                  <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 2 }}>
                    <TextField
                      label="Min. spacing (inches)"
                      type="number"
                      value={minSpacingInches}
                      onChange={e => setMinSpacingInches(e.target.value)}
                      slotProps={{ htmlInput: { min: 0.5, step: 0.5 } }}
                      sx={{ flex: 1 }}
                    />
                    <TextField
                      label="Planting depth (inches)"
                      type="number"
                      value={minDepthInches}
                      onChange={e => setMinDepthInches(e.target.value)}
                      slotProps={{ htmlInput: { min: 0.25, step: 0.25 } }}
                      sx={{ flex: 1 }}
                    />
                  </Stack>
                </Stack>
              </Box>

              {mutation.isError && (
                <Typography variant="body2" color="error">
                  Something went wrong. Please try again.
                </Typography>
              )}

              <Stack direction="row" sx={{ gap: 2, justifyContent: 'flex-end', mt: 1 }}>
                <Button onClick={() => navigate(-1)} color="inherit">
                  Cancel
                </Button>
                <Button
                  type="submit"
                  variant="contained"
                  color="secondary"
                  disabled={mutation.isPending}
                >
                  {mutation.isPending ? 'Adding…' : 'Add plant'}
                </Button>
              </Stack>
            </Stack>
          </form>
        </Card>
      </Container>
    </Box>
  )
}
