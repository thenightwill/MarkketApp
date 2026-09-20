import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LanguageService } from '../../core/i18n/language.service';
import { TasksPage } from './tasks-page';
import { UserTask } from './task.models';

// TASK MODULE (GenAI demonstration)
// These component tests check the two behaviours that matter for the review: the buttons offered for each
// status follow the state machine, and advancing a task sends the next status (never a skipped one).
const API = 'http://localhost:5203/api/tasks';

function task(id: string, status: UserTask['status'], title = `Tarea ${id}`): UserTask {
  return {
    id,
    userId: 'u1',
    title,
    description: 'detalle',
    status,
    dueDate: '2026-10-01T00:00:00Z',
    createdAt: '2026-09-01T00:00:00Z',
    updatedAt: null,
  };
}

describe('TasksPage', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(tasks: UserTask[]) {
    const fixture = TestBed.createComponent(TasksPage);
    fixture.detectChanges();
    http.expectOne(API).flush(tasks);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function buttons(root: HTMLElement): string[] {
    return Array.from(root.querySelectorAll('article button')).map((b) => b.textContent!.trim());
  }

  it('groups tasks by status', async () => {
    const fixture = await render([task('1', 'Pending'), task('2', 'InProgress'), task('3', 'Completed')]);

    const headings = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('h2')).map((h) => h.textContent);
    expect(headings.join(' ')).toContain('Pendiente');
    expect(headings.join(' ')).toContain('En progreso');
    expect(headings.join(' ')).toContain('Completada');
  });

  it('offers "Iniciar" only for pending tasks', async () => {
    const fixture = await render([task('1', 'Pending')]);

    expect(buttons(fixture.nativeElement)).toContain('Iniciar');
    expect(buttons(fixture.nativeElement)).not.toContain('Completar');
  });

  it('offers "Completar" only for tasks in progress', async () => {
    const fixture = await render([task('1', 'InProgress')]);

    expect(buttons(fixture.nativeElement)).toContain('Completar');
    expect(buttons(fixture.nativeElement)).not.toContain('Iniciar');
  });

  it('offers no transition for completed tasks', async () => {
    const fixture = await render([task('1', 'Completed')]);

    expect(buttons(fixture.nativeElement)).not.toContain('Iniciar');
    expect(buttons(fixture.nativeElement)).not.toContain('Completar');
  });

  it('sends the next status when a pending task is started', async () => {
    const fixture = await render([task('1', 'Pending')]);
    const start = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('article button')).find(
      (b) => b.textContent!.trim() === 'Iniciar',
    ) as HTMLButtonElement;

    start.click();

    const request = http.expectOne(`${API}/1`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.status).toBe('InProgress');
    expect(request.request.body.title).toBe('Tarea 1');
    request.flush(task('1', 'InProgress'));
    http.expectOne(API).flush([task('1', 'InProgress')]);
  });

  it('shows a friendly message when the server rejects a transition', async () => {
    const fixture = await render([task('1', 'Pending')]);
    const start = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('article button')).find(
      (b) => b.textContent!.trim() === 'Iniciar',
    ) as HTMLButtonElement;

    start.click();
    http.expectOne(`${API}/1`).flush({ code: 'INVALID_TASK_TRANSITION' }, { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.alert')?.textContent).toContain('Pendiente');
  });

  it('never sends a user id when creating a task', async () => {
    const fixture = await render([]);
    const component = fixture.componentInstance as unknown as {
      openCreate(): void;
      form: { patchValue(v: object): void };
      save(): void;
    };

    component.openCreate();
    component.form.patchValue({ title: 'Nueva', description: '', dueDate: '2026-10-05' });
    component.save();

    const request = http.expectOne(API);
    expect(request.request.method).toBe('POST');
    expect(Object.keys(request.request.body)).not.toContain('userId');
    expect(request.request.body.dueDate).toBe('2026-10-05T00:00:00.000Z');
    request.flush(task('9', 'Pending'));
    http.expectOne(API).flush([]);
  });

  it('shows an empty state when the user has no tasks', async () => {
    const fixture = await render([]);

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No tienes tareas');
  });

  it('switches status labels and action buttons to English', async () => {
    const fixture = await render([task('1', 'Pending'), task('2', 'InProgress')]);

    TestBed.inject(LanguageService).setLanguage('en');
    fixture.detectChanges();

    const headings = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('h2')).map((h) => h.textContent);
    expect(headings.join(' ')).toContain('Pending');
    expect(headings.join(' ')).toContain('In progress');
    expect(buttons(fixture.nativeElement)).toContain('Start');
    expect(buttons(fixture.nativeElement)).toContain('Complete');
    expect(buttons(fixture.nativeElement)).not.toContain('Iniciar');
  });
});
