// TASK MODULE (GenAI demonstration)
// The statuses mirror the backend enum. The transition table below is only used to decide which buttons
// to show; the backend remains the authority and rejects any illegal transition with INVALID_TASK_TRANSITION.
export type UserTaskStatus = 'Pending' | 'InProgress' | 'Completed';

export const TASK_STATUSES: UserTaskStatus[] = ['Pending', 'InProgress', 'Completed'];

export const NEXT_STATUS: Record<UserTaskStatus, UserTaskStatus | null> = {
  Pending: 'InProgress',
  InProgress: 'Completed',
  Completed: null,
};

export interface UserTask {
  id: string;
  userId: string;
  title: string;
  description: string;
  status: UserTaskStatus;
  dueDate: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateUserTaskPayload {
  title: string;
  description: string;
  dueDate: string;
}

export interface UpdateUserTaskPayload extends CreateUserTaskPayload {
  status: UserTaskStatus;
}
