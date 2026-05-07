'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { X } from 'lucide-react';
import { useEffect, useState } from 'react';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

const sizeClasses = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-xl',
};

export function Modal({
  isOpen,
  onClose,
  title,
  description,
  children,
  size = 'md',
}: ModalProps) {
  const [isMounted, setIsMounted] = useState(false);

  // Handle Escape key
  useEffect(() => {
    if (!isOpen) return;

    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    window.addEventListener('keydown', handleEscape);
    return () => window.removeEventListener('keydown', handleEscape);
  }, [isOpen, onClose]);

  // Prevent body scroll when modal is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      setIsMounted(true);
    } else {
      document.body.style.overflow = 'unset';
    }

    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  if (!isMounted) return null;

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50"
            aria-hidden="true"
          />

          {/* Modal */}
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ duration: 0.2 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4"
          >
            <motion.div
              className={`${sizeClasses[size]} w-full glassmorphism rounded-xl border overflow-hidden`}
              onClick={(e) => e.stopPropagation()}
              role="dialog"
              aria-modal="true"
              aria-labelledby="modal-title"
              aria-describedby={description ? 'modal-description' : undefined}
            >
              {/* Header */}
              <div className="flex items-center justify-between p-6 border-b border-zinc-800">
                <div className="flex-1">
                  <h2 id="modal-title" className="text-xl font-semibold text-foreground">
                    {title}
                  </h2>
                  {description && (
                    <p id="modal-description" className="text-sm text-zinc-400 mt-1">
                      {description}
                    </p>
                  )}
                </div>
                <motion.button
                  onClick={onClose}
                  className="p-2 rounded-lg hover:bg-zinc-800/50 transition-colors text-zinc-600 hover:text-foreground"
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                  aria-label="Close modal"
                >
                  <X className="w-5 h-5" />
                </motion.button>
              </div>

              {/* Content */}
              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 0.1 }}
                className="p-6"
              >
                {children}
              </motion.div>
            </motion.div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}

/**
 * Dialog trigger button with built-in modal management
 */
interface ModalTriggerProps {
  children: React.ReactNode;
  modalTitle: string;
  modalDescription?: string;
  modalContent: React.ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

export function ModalTrigger({
  children,
  modalTitle,
  modalDescription,
  modalContent,
  size,
}: ModalTriggerProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <motion.button
        onClick={() => setIsOpen(true)}
        whileHover={{ scale: 1.02 }}
        whileTap={{ scale: 0.98 }}
      >
        {children}
      </motion.button>

      <Modal
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        title={modalTitle}
        description={modalDescription}
        size={size}
      >
        {modalContent}
      </Modal>
    </>
  );
}
