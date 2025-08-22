import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../core/api';

@Component({
  selector: 'app-nuevo-partido-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatSelectModule, MatInputModule,
    MatButtonModule, MatIconModule
  ],
  templateUrl: './new-game-dialog.html',
  styleUrls: ['./new-game-dialog.scss']
})
export class NuevoPartidoDialogComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private dialogRef = inject(MatDialogRef<NuevoPartidoDialogComponent, any>);

  equipos: Array<{ id: number; nombre: string }> = [];
  loading = true;

  form = this.fb.group({
    equipoLocalId: [null as number | null, Validators.required],
    equipoVisitanteId: [null as number | null, Validators.required],
    minutos: [10, [Validators.required, Validators.min(1)]]
  });

  ngOnInit() {
    this.api.listarEquipos().subscribe({
      next: (ts: any[]) => {
        this.equipos = [...ts].sort((a, b) => a.nombre.localeCompare(b.nombre));
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  swap() {
    const home = this.form.value.equipoLocalId;
    const away = this.form.value.equipoVisitanteId;
    this.form.patchValue({ equipoLocalId: away, equipoVisitanteId: home });
  }

  cancel() { this.dialogRef.close(); }

  save() {
    if (this.form.invalid) return;
    const { equipoLocalId, equipoVisitanteId, minutos } = this.form.value;
    if (equipoLocalId === equipoVisitanteId) return;
    this.dialogRef.close({
      equipoLocalId,
      equipoVisitanteId,
      duracionPeriodoSegundos: Math.round(Number(minutos) * 60)
    });
  }
}
