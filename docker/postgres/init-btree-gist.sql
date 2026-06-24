-- Habilitar la extensión btree_gist requerida para el constraint EXCLUDE
-- anti double-booking en la tabla Bookings.
CREATE EXTENSION IF NOT EXISTS btree_gist;
