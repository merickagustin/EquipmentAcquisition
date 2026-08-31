import React from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack } from '@mui/material';

// The shared template every entity form plugs into — see docs/architecture.md's
// "The actual shared contract" for the full rationale. Owns layout, Enter-to-submit
// (via rendering the dialog's Paper as a <form>), disabled-while-submitting, and
// the single error Alert. Callers supply only fields (as children) and onSubmit.
export interface FormDialogProps {
  open: boolean;
  title: string;
  onClose: () => void;
  onSubmit: () => void | Promise<void>;
  submitting: boolean;
  error?: string | null;
  submitLabel?: string;
  children: React.ReactNode;
}

export function FormDialog({
  open,
  title,
  onClose,
  onSubmit,
  submitting,
  error,
  submitLabel = 'Save',
  children,
}: FormDialogProps) {
  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!submitting) onSubmit();
  };

  return (
    <Dialog
      open={open}
      onClose={(_event, reason) => {
        if (submitting || reason === 'backdropClick') return;
        onClose();
      }}
      fullWidth
      maxWidth="sm"
      PaperProps={{ component: 'form', onSubmit: handleSubmit }}
    >
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          {children}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={submitting}>
          Cancel
        </Button>
        <Button type="submit" variant="contained" disabled={submitting}>
          {submitLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
