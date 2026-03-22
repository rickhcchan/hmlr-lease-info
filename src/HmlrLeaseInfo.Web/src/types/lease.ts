export interface ParsedNoticeOfLease {
  entryNumber: number
  entryDate: string | null
  registrationDateAndPlanRef: string
  propertyDescription: string
  dateOfLeaseAndTerm: string
  lesseesTitle: string
  notes: string[]
}

export interface LeaseResponse {
  message: string
  lastSyncAt: string | null
}

export type QueryStatus = 'idle' | 'loading' | 'processing' | 'found' | 'notFound' | 'error'
