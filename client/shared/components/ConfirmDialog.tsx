import React from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle } from '@mui/material';

// Destructive-confirm — deletes. See docs/architecture.md's client/shared inventory.
export interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  onCancel: () => void;
  onConfirm: () => void | Promise<void>;
  confirming: boolean;
  error?: string | null;
}

export function ConfirmDialog({ open, title, message, onCancel, onConfirm, confirming, error }: ConfirmDialogProps) {
  return (
    <Dialog open={open} onClose={confirming ? undefined : onCancel} maxWidth="xs" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={confirming}>
          Cancel
        </Button>
        <Button onClick={onConfirm} color="error" variant="contained" disabled={confirming}>
          Delete
        </Button>
      </DialogActions>
    </Dialog>
  );
}
