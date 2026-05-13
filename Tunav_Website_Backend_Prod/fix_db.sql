-- Safe schema patch for existing databases that were created manually
-- (no __EFMigrationsHistory) and are missing newer columns.
-- This avoids dropping columns/data.

ALTER TABLE public.solutions
  ADD COLUMN IF NOT EXISTS "YoutubeUrl" character varying(500);

ALTER TABLE public.solutions
  ADD COLUMN IF NOT EXISTS "FunctionalitiesJson" character varying(8000);

ALTER TABLE public.solutions
  ADD COLUMN IF NOT EXISTS "AdvantagesJson" character varying(8000);

ALTER TABLE public.solutions
  ADD COLUMN IF NOT EXISTS "UseCasesJson" character varying(12000);
