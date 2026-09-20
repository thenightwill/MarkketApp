import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/config';
import { CreateUserTaskPayload, UserTask, UpdateUserTaskPayload } from './task.models';

// TASK MODULE (GenAI demonstration)
// No method takes a user id: the API derives the owner from the JWT that the interceptor attaches.
@Injectable({ providedIn: 'root' })
export class UserTaskService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_URL)}/tasks`;

  list(): Observable<UserTask[]> {
    return this.http.get<UserTask[]>(this.url);
  }

  create(payload: CreateUserTaskPayload): Observable<UserTask> {
    return this.http.post<UserTask>(this.url, payload);
  }

  update(id: string, payload: UpdateUserTaskPayload): Observable<UserTask> {
    return this.http.put<UserTask>(`${this.url}/${id}`, payload);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
