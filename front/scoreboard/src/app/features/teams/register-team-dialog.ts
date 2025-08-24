import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-register-team-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, MatIconModule, MatButtonModule
  ],
  templateUrl: './register-team-dialog.html',
  styleUrls: ['./register-team-dialog.css']
})
export class RegisterTeamDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<RegisterTeamDialogComponent, any>);

  form = this.fb.group({
    name: ['', Validators.required],
    color: ['#2e7dff'],
    players: this.fb.array<FormArray>([])
  });

  newPlayer = this.fb.group({
    number: [null as number | null],
    name: ['', Validators.required]
  });

  get players() { return this.form.get('players') as FormArray; }

  addPlayer() {
    const { number, name } = this.newPlayer.value;
    if (!name) return;
    this.players.push(this.fb.group({
      number: [number],
      name: [name, Validators.required]
    }));
    this.newPlayer.reset({ number: null, name: '' });
  }

  removePlayer(i: number) {
    this.players.removeAt(i);
  }

  cancel() {
    this.dialogRef.close();
  }

  save() {
    if (this.form.invalid) return;
    const value = this.form.value;
    const players = (value.players ?? []).map((p: any) => ({
      number: (p.number ?? null) === null || p.number === '' ? undefined : Number(p.number),
      name: String(p.name)
    }));
    this.dialogRef.close({
      name: value.name?.trim(),
      color: value.color?.trim() || undefined,
      players
    });
  }
}
