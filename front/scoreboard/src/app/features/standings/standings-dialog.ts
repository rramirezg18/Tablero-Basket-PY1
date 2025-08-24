import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

type Row = { id: number; name: string; color?: string; wins: number };

@Component({
  selector: 'app-standings-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  templateUrl: './standings-dialog.html',
  styleUrls: ['./standings-dialog.css']
})
export class StandingsDialogComponent {
  rows: Row[] = [];

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { rows: Row[] },
    private dialogRef: MatDialogRef<StandingsDialogComponent>
  ) {
    this.rows = (data?.rows ?? []).slice();
  }

  close() { this.dialogRef.close(); }
}
