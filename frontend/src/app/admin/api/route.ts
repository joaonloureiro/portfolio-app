import {NextResponse} from 'next/server';

export const dynamic = 'force-dynamic';

export function GET() {
  return NextResponse.json({type: 'about:blank', title: 'Admin route not found.', status: 404, errorKey: 'admin_route_not_found'}, {status: 404, headers: {'Cache-Control': 'no-store'}});
}
