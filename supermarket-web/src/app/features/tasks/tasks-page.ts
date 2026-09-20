import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { friendlyMessage } from '../../core/api/api-error';
import { LanguageService } from '../../core/i18n/language.service';
import { Alert } from '../../shared/components/alert';
import { NEXT_STATUS, TASK_STATUSES, UserTask, UserTaskStatus } from './task.models';
import { UserTaskService } from './task.service';

// TASK MODULE (GenAI demonstration)
// Design notes from the review of the generated code:
//  - The page only ever calls endpoints that are scoped to the logged-in user; it has no notion of "other users".
//  - Advancing a task sends the whole task back with the next status, mirroring the PUT contract, so a
//    stale screen cannot silently skip a step: the server validates the transition again.
//  - Server errors are shown as friendly messages, including INVALID_TASK_TRANSITION.
@Component({
  selector: 'app-tasks-page',
  imports: [ReactiveFormsModule, DatePipe, Alert],
  template: `
    <div class="page-header">
      <div>
        <h1>{{ i18n.t().tasks.title }}</h1>
        <p>{{ i18n.t().tasks.subtitle }}</p>
      </div>
      <button type="button" class="btn primary" (click)="openCreate()">{{ i18n.t().tasks.newTask }}</button>
    </div>

    <app-alert [message]="notice()" kind="success" />
    <app-alert [message]="error()" />

    @if (formOpen()) {
      <section class="card" [attr.aria-label]="i18n.t().tasks.formAriaLabel">
        <h2>{{ editing() ? i18n.t().tasks.editTask : i18n.t().tasks.newTask }}</h2>
        <form [formGroup]="form" (ngSubmit)="save()" novalidate>
          <div class="grid">
            <div class="field wide">
              <label for="t-title">{{ i18n.t().tasks.taskTitle }}</label>
              <input id="t-title" formControlName="title" [class.invalid]="form.controls.title.touched && form.controls.title.invalid">
              @if (form.controls.title.touched && form.controls.title.invalid) { <span class="error">{{ i18n.t().tasks.titleRequired }}</span> }
            </div>
            <div class="field wide">
              <label for="t-description">{{ i18n.t().tasks.description }}</label>
              <textarea id="t-description" rows="3" formControlName="description"></textarea>
            </div>
            <div class="field">
              <label for="t-due">{{ i18n.t().tasks.dueDate }}</label>
              <input id="t-due" type="date" formControlName="dueDate" [class.invalid]="form.controls.dueDate.touched && form.controls.dueDate.invalid">
              @if (form.controls.dueDate.touched && form.controls.dueDate.invalid) { <span class="error">{{ i18n.t().tasks.dueDateRequired }}</span> }
            </div>
          </div>
          <div class="actions" style="margin-top: 1rem">
            <button type="submit" class="btn primary" [disabled]="saving()">{{ saving() ? i18n.t().common.saving : i18n.t().common.save }}</button>
            <button type="button" class="btn" (click)="closeForm()">{{ i18n.t().common.cancel }}</button>
          </div>
        </form>
      </section>
    }

    @if (loading()) {
      <div class="card empty">{{ i18n.t().common.loading }}</div>
    } @else if (tasks().length === 0) {
      <div class="card empty">{{ i18n.t().tasks.empty }}</div>
    } @else {
      @for (group of groups(); track group.status) {
        <section class="card" [attr.aria-label]="group.label">
          <h2>{{ group.label }} <span class="badge">{{ group.tasks.length }}</span></h2>
          @if (group.tasks.length === 0) {
            <p class="muted">{{ i18n.t().tasks.noTasksInGroup }}</p>
          }
          @for (task of group.tasks; track task.id) {
            <article class="task">
              <div class="info">
                <strong>{{ task.title }}</strong>
                @if (task.description) { <div class="muted">{{ task.description }}</div> }
                <div class="muted">{{ i18n.t().tasks.due }} {{ task.dueDate | date: 'dd/MM/yyyy' : 'UTC' }}</div>
              </div>
              <div class="actions">
                @if (nextLabel(task); as label) {
                  <button type="button" class="btn small primary" (click)="advance(task)">{{ label }}</button>
                }
                <button type="button" class="btn small" (click)="openEdit(task)">{{ i18n.t().common.edit }}</button>
                <button type="button" class="btn small danger" (click)="remove(task)">{{ i18n.t().tasks.delete }}</button>
              </div>
            </article>
          }
        </section>
      }
    }
  `,
  styles: `
    .task { display: flex; flex-wrap: wrap; justify-content: space-between; gap: .75rem; padding: .75rem 0; border-top: 1px solid var(--border); }
    .task .info { flex: 1; min-width: 200px; }
  `,
})
export class TasksPage implements OnInit {
  private readonly service = inject(UserTaskService);

  protected readonly i18n = inject(LanguageService);
  protected readonly tasks = signal<UserTask[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly formOpen = signal(false);
  protected readonly editing = signal<UserTask | null>(null);

  protected readonly groups = computed(() =>
    TASK_STATUSES.map((status) => ({
      status,
      label: this.i18n.t().tasks.statuses[status],
      tasks: this.tasks().filter((t) => t.status === status),
    })),
  );

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(2000)],
    dueDate: ['', Validators.required],
  });

  ngOnInit(): void {
    this.load();
  }

  protected nextLabel(task: UserTask): string | null {
    const next = NEXT_STATUS[task.status];
    if (!next) {
      return null;
    }
    return next === 'InProgress' ? this.i18n.t().tasks.actions.start : this.i18n.t().tasks.actions.complete;
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.form.reset({ title: '', description: '', dueDate: '' });
    this.formOpen.set(true);
    this.notice.set(null);
  }

  protected openEdit(task: UserTask): void {
    this.editing.set(task);
    this.form.reset({
      title: task.title,
      description: task.description,
      dueDate: task.dueDate.slice(0, 10),
    });
    this.formOpen.set(true);
    this.notice.set(null);
  }

  protected closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload = { ...value, dueDate: new Date(`${value.dueDate}T00:00:00Z`).toISOString() };
    const current = this.editing();
    const request = current
      ? this.service.update(current.id, { ...payload, status: current.status })
      : this.service.create(payload);

    this.saving.set(true);
    this.error.set(null);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(current ? this.i18n.t().tasks.updated : this.i18n.t().tasks.created);
        this.closeForm();
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
      },
    });
  }

  protected advance(task: UserTask): void {
    const next = NEXT_STATUS[task.status];

    if (!next) {
      return;
    }

    this.error.set(null);
    this.service
      .update(task.id, {
        title: task.title,
        description: task.description,
        dueDate: task.dueDate,
        status: next,
      })
      .subscribe({
        next: () => this.load(),
        error: (err: unknown) => this.error.set(friendlyMessage(err, this.i18n.t().errors)),
      });
  }

  protected remove(task: UserTask): void {
    if (!confirm(this.i18n.t().tasks.confirmDelete(task.title))) {
      return;
    }

    this.error.set(null);
    this.service.remove(task.id).subscribe({
      next: () => {
        this.notice.set(this.i18n.t().tasks.deleted);
        this.load();
      },
      error: (err: unknown) => this.error.set(friendlyMessage(err, this.i18n.t().errors)),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.service.list().subscribe({
      next: (items) => {
        this.tasks.set(items);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(friendlyMessage(err, this.i18n.t().errors));
        this.loading.set(false);
      },
    });
  }
}
