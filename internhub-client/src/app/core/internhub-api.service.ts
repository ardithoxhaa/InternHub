import { HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';

interface AuthUser {
  token: string;
}

@Injectable({ providedIn: 'root' })
export class InternHubApiService {
  readonly api = 'http://localhost:5170/api';
  readonly hubUrl = 'http://localhost:5170/hubs/team-chat';

  auth(user: AuthUser | null): { headers: HttpHeaders } {
    return { headers: new HttpHeaders({ Authorization: `Bearer ${user?.token ?? ''}` }) };
  }
}
