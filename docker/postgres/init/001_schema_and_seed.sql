CREATE TABLE IF NOT EXISTS document (
  id INTEGER PRIMARY KEY,
  url TEXT,
  idxTime TIMESTAMP,
  creationTime TIMESTAMP
);

CREATE TABLE IF NOT EXISTS word (
  id INTEGER PRIMARY KEY,
  name TEXT
);

CREATE TABLE IF NOT EXISTS occ (
  wordId INTEGER,
  docId INTEGER,
  FOREIGN KEY (wordId) REFERENCES word(id),
  FOREIGN KEY (docId) REFERENCES document(id)
);

CREATE INDEX IF NOT EXISTS word_index ON occ (wordId);
CREATE INDEX IF NOT EXISTS doc_index ON occ (docId);

TRUNCATE TABLE occ;
TRUNCATE TABLE word;
TRUNCATE TABLE document;

INSERT INTO document (id, url, idxTime, creationTime) VALUES
  (1, '/docs/mail-1.txt', NOW(), NOW()),
  (2, '/docs/mail-2.txt', NOW(), NOW()),
  (3, '/docs/report-1.txt', NOW(), NOW()),
  (4, '/docs/report-2.txt', NOW(), NOW());

INSERT INTO word (id, name) VALUES
  (1, 'SoCal'),
  (2, 'socal'),
  (3, 'energy'),
  (4, 'market'),
  (5, 'Search'),
  (6, 'search'),
  (7, 'pipeline');

INSERT INTO occ (wordId, docId) VALUES
  (1, 1),
  (3, 1),
  (4, 1),
  (2, 2),
  (3, 2),
  (5, 3),
  (6, 4),
  (7, 4);
